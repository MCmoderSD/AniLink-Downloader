package de.MCmoderSD.utilities;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import de.MCmoderSD.enums.Language;
import de.MCmoderSD.objects.SubStream;

import java.io.*;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.HashSet;
import java.util.concurrent.atomic.AtomicInteger;

import static de.MCmoderSD.utilities.Helper.*;

@SuppressWarnings({"unused", "FieldCanBeLocal"})
public class SubtitleExtractor {

    // Singleton Instance
    private static SubtitleExtractor instance;

    // Attributes
    private final File mkvExtract;
    private final File mkvInfo;
    private final File mkvMerge;
    private final File mkvPropEdit;
    private final ObjectMapper mapper;

    // Constructor
    private SubtitleExtractor(File mkvToolNixPath) {

        // Check if the path is valid
        try {
            checkDirectory(mkvToolNixPath);
        } catch (IOException e) {
            throw new IllegalArgumentException("Invalid MKVToolNix path: " + mkvToolNixPath.getAbsolutePath(), e);
        }

        // Initialize MKVToolNix tools
        mkvExtract = new File(mkvToolNixPath, "mkvextract.exe");
        mkvInfo = new File(mkvToolNixPath, "mkvinfo.exe");
        mkvMerge = new File(mkvToolNixPath, "mkvmerge.exe");
        mkvPropEdit = new File(mkvToolNixPath, "mkvpropedit.exe");

        // Validate tools
        File[] tools = {mkvExtract, mkvInfo, mkvMerge, mkvPropEdit};
        for (var tool : tools) {
            try {
                if (!isValidTool(tool)) throw new IllegalStateException("Invalid or incompatible tool: " + tool.getAbsolutePath());
            } catch (IOException | InterruptedException e) {
                throw new RuntimeException("Error validating tool: " + tool.getAbsolutePath(), e);
            }
        }

        // Initialize the ObjectMapper
        mapper = new ObjectMapper();

        // Log successful initialization
        log("MKVToolNix tools initialized successfully.");
    }

    // Initialization Method
    public static SubtitleExtractor init(File mkvToolNixPath) {
        if (instance == null) instance = new SubtitleExtractor(mkvToolNixPath);
        return instance;
    }

    // Get Instance Method
    public static SubtitleExtractor getInstance() {
        if (instance == null) throw new IllegalStateException("SubtitleExtractor not initialized. Call init() first.");
        return instance;
    }

    // Tool Validator
    private static boolean isValidTool(File tool) throws IOException, InterruptedException {

        // Check if the tool exists, is a file, and is executable
        if (!isReadAble(tool) || !isExecutable(tool)) throw new IOException("Tool is not readable or executable: " + tool.getAbsolutePath());

        // Get Tool Name
        var toolName = tool.getName();
        toolName = toolName.substring(0, toolName.lastIndexOf("."));

        // Check version compatibility
        ProcessBuilder processBuilder = buildProcess(tool, "--version");
        Process process = processBuilder.start();
        String output = readStream(process.getInputStream()).trim();
        var exit = process.waitFor();
        if (exit != 0) return false;
        return output.toLowerCase().startsWith((toolName + " v").toLowerCase());
    }

    // Process Builder Helper
    private static ProcessBuilder buildProcess(File tool, String... args) {
        ProcessBuilder processBuilder = new ProcessBuilder();
        processBuilder.command(tool.getAbsolutePath());
        for (var arg : args) if (arg != null && !arg.isBlank()) processBuilder.command().add(arg);
        return processBuilder;
    }

    // Process Stream Reader
    private static String readStream(InputStream inputStream) throws IOException {
        BufferedReader bufferedReader = new BufferedReader(new InputStreamReader(inputStream, StandardCharsets.UTF_8));
        StringBuilder stringBuilder = new StringBuilder();
        String line;
        while ((line = bufferedReader.readLine()) != null) stringBuilder.append(line).append("\n");
        return stringBuilder.toString();
    }

    // Recursively build a unique file name for the subtitle file
    private static ArrayList<String> buildFileName(ArrayList<String> outputFiles, File outputDir, String baseName, SubStream subStream, int count) {

        // Build the file name based on the subStream properties
        StringBuilder fileName = new StringBuilder(new File(outputDir, baseName).getAbsolutePath());
        if (subStream.isDefaultTrack()) fileName.append(".default");
        fileName.append(".").append(subStream.getLanguage().getAlpha2());
        if (subStream.isForcedTrack()) fileName.append(".forced");
        if (count > 0) fileName.append(" (").append(count).append(")");
        fileName.append(".srt");

        // Ensure the file name is unique
        if (outputFiles.contains(fileName.toString())) return buildFileName(outputFiles, outputDir, baseName, subStream, count + 1);
        outputFiles.add(fileName.toString());
        return outputFiles;
    }

    // Parse MKV file tracks using mkvmerge
    private JsonNode parseTracks(File mkvFile) throws IOException, InterruptedException {
        log(String.format("Running mkvmerge -J on %s", mkvFile.getAbsolutePath()));
        ProcessBuilder processBuilder = buildProcess(mkvMerge, "-J", mkvFile.getAbsolutePath());
        Process process = processBuilder.start();
        String output = readStream(process.getInputStream()).trim();
        var exit = process.waitFor();
        if (exit != 0) throw new IOException("mkvmerge failed: " + readStream(process.getErrorStream()));

        // Parse JSON output and get tracks
        log("Parsing JSON output from mkvmerge");
        return mapper.readTree(output).get("tracks");
    }

    // Extract a specific track from the MKV file using mkvextract
    private void extractTrack(File mkvFile, int trackId, String outFile) throws IOException, InterruptedException {
        log(String.format("Extracting track %d to %s", trackId, outFile));
        ProcessBuilder processBuilder = buildProcess(mkvExtract, "tracks", mkvFile.getAbsolutePath(), trackId + ":" + outFile);
        Process process = processBuilder.start();
        process.getErrorStream().transferTo(System.err);
        if (process.waitFor() != 0) throw new IOException("mkvextract failed for track " + trackId);
    }

    public void extract(File mkvFile) throws IOException, InterruptedException {

        // Parse MKV file tracks
        JsonNode tracks = parseTracks(mkvFile);

        // Generate Subtitle Streams
        ArrayList<SubStream> subStreams = new ArrayList<>();
        for (var track : tracks) {
            if (!track.get("type").asText().equalsIgnoreCase("subtitles")) continue;

            // Create SubStream object
            SubStream subStream = new SubStream(track);
            subStreams.add(subStream);

            // Debug output
            log(String.format("Track ID: %d, Default: %b, Enabled: %b, Forced: %b, Language: %s",
                    subStream.getId(),
                    subStream.isDefaultTrack(),
                    subStream.isEnabledTrack(),
                    subStream.isForcedTrack(),
                    subStream.getLanguage().getName()
            ));
        }

        // If no subtitle streams found, exit
        if (subStreams.isEmpty()) return;
        log(String.format("Found %d subtitle tracks in %s", subStreams.size(), mkvFile.getName()));

        // Extract each subtitle track
        ArrayList<String> outputFiles = new ArrayList<>();
        AtomicInteger threadCount = new AtomicInteger(subStreams.size());
        for (var subStream : subStreams) {

            // Skip if track is not enabled
            if (!subStream.isEnabledTrack()) {
                log(String.format("Skipping disabled track %d (%s)%n", subStream.getId(), subStream.getLanguage().getName()));
                continue;
            }

            // Build the output file name
            outputFiles = buildFileName(outputFiles, TEMP_DIR, mkvFile.getName().replaceAll("(?i)\\.mkv$", ""), subStream, 0);
            String outputFile = outputFiles.getLast();

            // Extract the track
            new Thread(() -> {
                try {
                    File tempFile = new File(TEMP_DIR, hash(outputFile) + ".srt");
                    File destinationFile = new File(outputFile);
                    extractTrack(mkvFile, subStream.getId(), tempFile.getAbsolutePath());
                    moveFile(tempFile, destinationFile);
                    threadCount.decrementAndGet();
                } catch (IOException | InterruptedException e) {
                    threadCount.decrementAndGet();
                    throw new RuntimeException(e);
                }
            }, mkvFile.getName() + " - Track " + subStream.getId()).start();
        }

        // Wait for all extraction threads to finish
        while (threadCount.get() > 0) holdUp(threadCount.get());

        // Ensure all output files are valid
        HashSet<File> outputFileFiles = new HashSet<>();
        for (String outputFile : outputFiles) {
            File file = new File(outputFile);
            checkFile(file);
            outputFileFiles.add(file);
        }

        // Delete duplicate subtitle files
        HashSet<Language> languages = new HashSet<>();
        for (var subStream : subStreams) languages.add(subStream.getLanguage());
        HashMap<Language, File[]> languageSubtitles = new HashMap<>();
        for (var lang : languages) {

            ArrayList<File> normalSubtitles = new ArrayList<>();
            ArrayList<File> forcedSubtitles = new ArrayList<>();
            ArrayList<File> defaultSubtitles = new ArrayList<>();

            for (var subFile : outputFileFiles) {

                // Process subtitle file name
                String subName = subFile.getName().toLowerCase();
                if (subName.endsWith(").srt")) subName = subName.substring(0, subName.lastIndexOf(" (")) + ".srt";

                // Check if the subtitle file matches the language
                String languagePart;
                if (subName.endsWith(".forced.srt")) languagePart = subName.substring(subName.length() - 13, subName.length() - 11);
                else languagePart = subName.substring(subName.length() - 6, subName.length() - 4);
                if (!languagePart.equalsIgnoreCase(lang.getAlpha2())) continue;

                // Forced subtitles
                if (subName.contains("forced.srt")) {
                    forcedSubtitles.add(subFile);
                    continue;
                }

                // Default subtitles
                if (subName.contains(".default.")) {
                    defaultSubtitles.add(subFile);
                    continue;
                }

                // Normal subtitles
                normalSubtitles.add(subFile);
            }

            // Sort subtitles by file size (largest first)
            normalSubtitles.sort((a, b) -> Long.compare(b.length(), a.length()));
            forcedSubtitles.sort((a, b) -> Long.compare(b.length(), a.length()));
            defaultSubtitles.sort((a, b) -> Long.compare(b.length(), a.length()));

            // Select the largest subtitle file for each category
            File[] selectedSubtitles = new File[3];
            selectedSubtitles[0] = normalSubtitles.isEmpty() ? null : normalSubtitles.getFirst();
            selectedSubtitles[1] = forcedSubtitles.isEmpty() ? null : forcedSubtitles.getFirst();
            selectedSubtitles[2] = defaultSubtitles.isEmpty() ? null : defaultSubtitles.getFirst();
            languageSubtitles.put(lang, selectedSubtitles);
        }

        // Add subtitles to the white list
        HashSet<File> whiteList = new HashSet<>();
        for (File[] subs : languageSubtitles.values()) {

            // Add to white list
            var normal = subs[0];
            var forced = subs[1];
            var defaultSub = subs[2];

            // Check if the files are valid
            if (normal != null) {
                checkFile(normal);
                whiteList.add(normal);
            }

            // Add forced subtitle if it exists
            if (forced != null) {
                checkFile(forced);
                whiteList.add(forced);
            }

            // Add default subtitle if it exists
            if (defaultSub != null) {
                checkFile(defaultSub);
                whiteList.add(defaultSub);
            }
        }

        // Remove non-white listed files
        for (var file : outputFileFiles) {
            if (!whiteList.contains(file)) {
                log(String.format("Deleting file: %s", file.getAbsolutePath()));
                deleteFile(file);
            } else log(String.format("Skipping file: %s", file.getAbsolutePath()));
        }

        // Rename subtitle files
        for (var file : whiteList) {
            String fileName = file.getName();
            if (fileName.endsWith(").srt")) {
                fileName = fileName.substring(0, fileName.lastIndexOf(" (")) + ".srt";
                File destination = new File(file.getParentFile(), fileName);
                moveFile(file, destination);
                log(String.format("Moved file %s to %s", file.getAbsolutePath(), destination.getAbsolutePath()));
                file = destination;
            }

            // Move to the output directory
            File destination = new File(mkvFile.getParentFile(), file.getName());
            moveFile(file, destination);
            log(String.format("Moved file %s to %s", file.getAbsolutePath(), destination.getAbsolutePath()));
        }
    }
}