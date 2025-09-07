package de.MCmoderSD.objects;

import de.MCmoderSD.debrid.objects.Download;

import java.io.*;
import java.net.URISyntaxException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.Comparator;
import java.util.Objects;
import java.util.UUID;
import java.util.stream.Stream;

import static de.MCmoderSD.main.Main.PASSWORD;
import static de.MCmoderSD.main.Main.SEVEN_ZIP_PATH;
import static de.MCmoderSD.utilities.Helper.*;

public record RarArchive(ArrayList<Download> downloads) {

    // Constructor

    private static File downloadFile(File tempDir, Download download) throws IOException {

        // Create the file in the temporary directory
        File file = new File(tempDir, download.getName());
        deleteFile(file);
        if (!file.createNewFile() || !isReadAble(file) || !isWriteAble(file))
            throw new IOException("Failed to create file: " + file.getAbsolutePath());

        // Open the input stream from the download
        try (
                BufferedInputStream bis = new BufferedInputStream(download.openStream());
                FileOutputStream fos = new FileOutputStream(file);
                BufferedOutputStream bos = new BufferedOutputStream(fos)
        ) {
            bis.transferTo(bos);
        } catch (IOException | URISyntaxException e) {
            throw new IOException("Failed to download file: " + download.getDownloadLink(), e);
        }

        // Return the downloaded file
        return file;
    }

    private static void deleteTempFiles(File tempDir, File mkvFile) throws IOException {
        for (var file : Objects.requireNonNull(tempDir.listFiles())) {
            if (file.equals(mkvFile)) continue;
            if (file.isDirectory()) {
                deleteTempFiles(file, mkvFile); // Recursively delete contents
                if (!file.delete()) throw new IOException("Failed to delete directory: " + file.getAbsolutePath());
            } else if (!file.delete()) throw new IOException("Failed to delete file: " + file.getAbsolutePath());
        }
    }

    // Download and extract MKV file from RAR archive
    public File downloadAndExtractMKV() throws IOException {

        // Create a temporary directory for the downloads
        File tempDir = new File(TEMP_DIR, UUID.randomUUID().toString());
        deleteDirectory(tempDir);
        if (!tempDir.mkdirs())
            throw new IOException("Failed to create temporary directory: " + tempDir.getAbsolutePath());
        checkDirectory(tempDir);
        tempDir.deleteOnExit(); // Ensure the temporary directory is deleted on exit
        log("Temporary directory created: " + tempDir.getAbsolutePath());

        // Download each part and store it in the parts array
        ArrayList<File> partFiles = new ArrayList<>();
        for (var i = 0; i < downloads.size(); i++) {

            // Get the download object
            Download download = downloads.get(i);
            log("Downloading part " + (i + 1) + " of " + downloads.size() + ": " + download.getName());

            // Download the file
            File partFile;
            try {
                partFile = downloadFile(tempDir, download);
            } catch (IOException e) {
                throw new IOException("Failed to download part " + (i + 1) + ": " + download.getDownloadLink(), e);
            }

            // Add the part file to the list
            partFiles.add(partFile);
            log("Part " + (i + 1) + " downloaded: " + partFile.getAbsolutePath());
        }

        // Use the first part as the entry point for extraction
        File firstPart = partFiles.getFirst();

        // Build the command to extract the files using 7-Zip
        ArrayList<String> command = new ArrayList<>();
        command.add(SEVEN_ZIP_PATH);
        command.add("x");
        command.add("-p" + PASSWORD);
        command.add(firstPart.getAbsolutePath());
        command.add("-o" + tempDir);
        command.add("-y");

        // Add the part files to the command
        ProcessBuilder processBuilder = new ProcessBuilder(command);
        processBuilder.redirectErrorStream(true);
        Process process = processBuilder.start();

        // Read the output of the process
        BufferedReader reader = new BufferedReader(new InputStreamReader(process.getInputStream()));
        String line;
        while ((line = reader.readLine()) != null) log(line);

        // Wait for the process to finish and check the exit code
        int exitCode;
        try {
            exitCode = process.waitFor();
            if (exitCode != 0) throw new IOException("7-Zip extraction failed with exit code " + exitCode);
            else log("7-Zip extraction completed successfully with exit code: " + exitCode);
        } catch (InterruptedException e) {
            throw new IOException("7-Zip extraction was interrupted", e);
        }

        // Get all files in the temporary directory
        ArrayList<File> tempFiles = new ArrayList<>();
        try (Stream<Path> stream = Files.walk(tempDir.toPath())) {
            stream.filter(Files::isRegularFile).forEach(path -> tempFiles.add(path.toFile()));
        }

        // Sort the files by size to find the mkv file
        File mkvFile;
        tempFiles.sort(Comparator.comparingLong(File::length));
        tempFiles.removeIf(file -> !file.getName().endsWith(".mkv"));
        if (tempFiles.isEmpty()) throw new FileNotFoundException("No MKV file found in the extracted files");
        else mkvFile = tempFiles.getFirst();

        // Move the mkv file to the temporary directory
        if (!moveFile(mkvFile, new File(tempDir, mkvFile.getName())))
            throw new IOException("Failed to move MKV file to temporary directory: " + mkvFile.getAbsolutePath());

        // Clean up the temporary files
        try {
            deleteTempFiles(tempDir, mkvFile);
        } catch (IOException e) {
            throw new IOException("Failed to clean up temporary files", e);
        }

        // Return the MKV file
        return mkvFile;
    }
}