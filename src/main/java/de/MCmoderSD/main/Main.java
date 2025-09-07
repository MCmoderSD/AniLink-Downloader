package de.MCmoderSD.main;

import com.fasterxml.jackson.databind.JsonNode;
import de.MCmoderSD.core.Loader;
import de.MCmoderSD.json.JsonUtility;
import de.MCmoderSD.utilities.SubtitleExtractor;

import java.io.File;
import java.io.IOException;
import java.net.URISyntaxException;
import java.nio.file.Files;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.HashSet;
import java.util.Scanner;
import java.util.concurrent.atomic.AtomicInteger;

import static de.MCmoderSD.utilities.DirectoryCleaner.deleteEmptyDirectory;
import static de.MCmoderSD.utilities.DirectoryCleaner.moveFilesRecursively;
import static de.MCmoderSD.utilities.Helper.*;

public class Main {

    // Version
    public static final String VERSION = "1.0.0";                               // Version of the

    // Constants
    public static boolean DEBUG = false;                                        // Default False
    public static long DELAY = 1000;                                            // Recommended 1000ms (1 second)
    public static String SEVEN_ZIP_PATH = "C:\\Program Files\\7-Zip\\7z.exe";   // Default 7-Zip Path
    public static String MKV_TOOL_NIX_PATH = "C:\\Program Files\\MKVToolNix";   // Default MKVToolNix Path
    public static String PASSWORD = "www.anime-loads.org";                      // Default Anime-Loads.org Password

    // Attributes
    private static Loader loader;

    // Main Method
    public static void main(String[] args) {

        // Get arguments
        ArrayList<String> argList = new ArrayList<>(Arrays.asList(args));

        // Load Config
        JsonNode config;
        try {
            config = JsonUtility.getInstance().load("/config.json");
        } catch (IOException | URISyntaxException e) {
            throw new RuntimeException("Failed to load config.json: " + e.getMessage(), e);
        }

        // Set Constats from Config
        String apikey = config.get("apikey").asText();
        if (config.has("debug")) DEBUG = config.get("debug").asBoolean();
        if (config.has("delay")) DELAY = config.get("delay").asLong();
        if (config.has("7zip")) SEVEN_ZIP_PATH = config.get("7zip").asText();
        if (config.has("mkvToolNix")) MKV_TOOL_NIX_PATH = config.get("mkvToolNix").asText();
        if (config.has("password")) PASSWORD = config.get("password").asText();

        // Help Command
        if (containsArg(argList, "--help", "-h")) {
            System.out.println("Usage: java -jar Anime-Loads-Downloader.jar [options]");
            System.out.println("Options:");
            System.out.println("  --help,           -h      Show this help message");
            System.out.println("  --version,        -v      Show version information");
            System.out.println("  --debug,          -d      Enable debug mode");
            System.out.println("  --cleanup,        -c      Clean up files and directories");
            System.out.println("  --delay,          -w      Override delay between requests (in milliseconds)");
            System.out.println("  --7zip,           -z      Override 7-Zip path");
            System.out.println("  --mkv-tool-nix,   -t      Override MKVToolNix path");
            System.out.println("  --password,       -p      Override Anime-Loads.org password");
            System.out.println("  --subtitle,       -s      Run subtitle extractor in current directory");
            System.out.println("  --apikey,         -a      Override API key");
            System.out.println("  --import,         -i      Import files from a text file");
            System.out.println("  --manual,         -m      Run in manual mode");
            System.exit(0);
        }

        // Version Command
        if (containsArg(argList, "--version", "-v")) {
            System.out.println("Anime-Loads Downloader v: " + VERSION);
            System.exit(0);
        }

        // Debug mode
        if (containsArg(argList, "--debug", "-d")) DEBUG = true;
        log("Debug mode enabled!");

        // Clean Up
        if (containsArg(argList, "--cleanup") || containsArg(argList, "-c")) {
            System.out.println("Cleaning up files and directories...");
            File root = getWorkingDirectory();
            try {
                moveFilesRecursively(root);
                deleteEmptyDirectory(root);
            } catch (IOException e) {
                throw new RuntimeException("Failed to clean up directory: " + root.getAbsolutePath() + ". Error: " + e.getMessage(), e);
            }
            System.exit(0);
        }

        // Override Delay
        if (containsArg(argList, "--delay", "-w")) {
            var argIndex = 0;
            if (argList.contains("--delay")) argIndex = argList.indexOf("--delay");
            else if (argList.contains("-w")) argIndex = argList.indexOf("-w");
            if (argIndex + 1 < argList.size()) {
                try {

                    // Parse delay value
                    DELAY = Long.parseLong(argList.get(argIndex + 1));
                    System.out.println("Delay set to " + DELAY + "ms");

                    // Remove Argument and value
                    argList.remove(argIndex);
                    argList.remove(argIndex + 1);

                } catch (NumberFormatException e) {
                    System.err.println("Invalid delay value. Please provide a valid number.");
                    System.exit(1);
                }
            }
        }

        // Override 7-Zip Path
        if (containsArg(argList, "--7zip", "-z")) {
            var argIndex = 0;
            if (argList.contains("--7zip")) argIndex = argList.indexOf("--7zip");
            else if (argList.contains("-z")) argIndex = argList.indexOf("-z");
            if (argIndex + 1 < argList.size()) {

                // Parse 7-Zip path
                SEVEN_ZIP_PATH = argList.get(argIndex + 1);
                System.out.println("7-Zip path set to " + SEVEN_ZIP_PATH);

                // Remove Argument and value
                argList.remove(argIndex);
                argList.remove(argIndex + 1);
            }
        }

        // Validate 7-Zip Path
        try {
            if (!validate7zip(SEVEN_ZIP_PATH)) throw new RuntimeException("7-Zip path is not valid or not executable: " + SEVEN_ZIP_PATH);
        } catch (IOException | InterruptedException e) {
            throw new RuntimeException("Failed to validate 7-Zip path: " + SEVEN_ZIP_PATH + "Error: " + e.getMessage(), e);
        }

        // MKVToolNix Path Command
        if (containsArg(argList, "--mkv-tool-nix", "-t")) {
            var argIndex = 0;
            if (argList.contains("--mkv-tool-nix")) argIndex = argList.indexOf("--mkv-tool-nix");
            else if (argList.contains("-t")) argIndex = argList.indexOf("-t");
            if (argIndex + 1 < argList.size()) {

                // Set MKVToolNix path
                MKV_TOOL_NIX_PATH = argList.get(argIndex + 1);
                System.out.println("Using MKVToolNix path: " + MKV_TOOL_NIX_PATH);

                // Remove Argument and value
                argList.remove(argIndex);
                argList.remove(argIndex + 1);
            }
        }

        // Override Password
        if (containsArg(argList, "--password", "-p")) {
            var argIndex = 0;
            if (argList.contains("--password")) argIndex = argList.indexOf("--password");
            else if (argList.contains("-p")) argIndex = argList.indexOf("-p");
            if (argIndex + 1 < argList.size()) {

                // Parse Password
                PASSWORD = argList.get(argIndex + 1);
                System.out.println("Password set to " + PASSWORD);

                // Remove Argument and value
                argList.remove(argIndex);
                argList.remove(argIndex + 1);
            }
        }

        // Subtitle Extractor
        if (containsArg(argList, "--subtitle", "-s")) {
            System.out.println("Running Subtitle Extractor in current directory...");
            extractSubtitles();
            System.exit(0);
        }

        // Override API Key
        if (containsArg(argList, "--apikey", "-a")) {
            var argIndex = 0;
            if (argList.contains("--apikey")) argIndex = argList.indexOf("--apikey");
            else if (argList.contains("-a")) argIndex = argList.indexOf("-a");
            if (argIndex + 1 < argList.size()) {

                // Parse API Key
                apikey = argList.get(argIndex + 1);
                System.out.println("API Key set to " + apikey);

                // Remove Argument and value
                argList.remove(argIndex);
                argList.remove(argIndex + 1);
            }
        }

        // Check if API Key is provided
        if (apikey == null || apikey.isBlank()) {
            System.err.println("API Key is required. Please provide it using --apikey or -a.");
            System.exit(1);
        }

        // Initialize Loader
        loader = new Loader(apikey);

        // Import Mode
        if (containsArg(argList, "--import", "-i")) {
            var argIndex = 0;
            if (argList.contains("--import")) argIndex = argList.indexOf("--import");
            else if (argList.contains("-i")) argIndex = argList.indexOf("-i");
            if (argIndex + 1 < argList.size()) {

                // Parse file path
                String filePath = argList.get(argIndex + 1);
                System.out.println("Importing from file: " + filePath);

                // Run import mode
                importMode(new File(filePath));
                return;
            }
        }

        // Manual Mode
        if (containsArg(argList, "--manual", "-m")) {
            manualMode();
            return;
        }

        // Default Mode
        defaultMode();
    }

    private static boolean containsArg(ArrayList<String> args, String... arg) {
        for (String a : arg) if (args.contains(a)) return true;
        return false;
    }

    private static boolean validate7zip(String path) throws IOException, InterruptedException {
        File zip = new File(path);
        if (!isReadAble(zip) || !isExecutable(zip)) throw new IOException("7-Zip path is not valid or not executable: " + path);
        ProcessBuilder processBuilder = new ProcessBuilder(path);
        Process process = processBuilder.start();
        String output  = new String(process.getInputStream().readAllBytes());
        process.waitFor();
        return output.trim().startsWith("7-Zip");
    }

    private static void extractSubtitles() {

        // Get current working directory
        File workingDirectory = getWorkingDirectory();
        System.out.printf("Working directory: %s%n%n", workingDirectory.getAbsolutePath());

        // Find all .mkv files in the current directory
        File[] mkvFiles = workingDirectory.listFiles((dir, name) -> name.toLowerCase().endsWith(".mkv"));
        if (mkvFiles == null || mkvFiles.length == 0) throw new RuntimeException("No .mkv files found in the current directory: " + workingDirectory.getAbsolutePath());

        // Init SubtitleExtractor
        log("Initializing SubtitleExtractor...");
        SubtitleExtractor subtitleExtractor = SubtitleExtractor.init(new File(MKV_TOOL_NIX_PATH));

        // Process each .mkv file in a separate thread
        AtomicInteger completedThreads = new AtomicInteger(0);
        AtomicInteger failedThreads = new AtomicInteger(0);
        for (var mkvFile : mkvFiles) {
            var name = mkvFile.getName();
            new Thread(() -> {
                try {
                    subtitleExtractor.extract(mkvFile);
                    completedThreads.incrementAndGet();
                    System.out.println("Successfully processed file: " + name);
                } catch (IOException | InterruptedException e) {
                    System.err.println("Error processing file " + name + ": " + e.getMessage());
                    failedThreads.incrementAndGet();
                }
            }, name
            ).start();
        }

        // Wait for all threads to finish
        while (mkvFiles.length - completedThreads.get() - failedThreads.get() > 0) holdUp(mkvFiles.length);

        // Print summary
        System.out.println("\nProcessing completed!");
        System.out.println("Completed: " + completedThreads.get() + " | Failed: " + failedThreads.get());
    }

    private static void importMode(File inputFile) {

        // Check if file exists
        try {
            isReadAble(inputFile);
        } catch (IOException e) {
            throw new RuntimeException("Failed to check file: " + inputFile.getAbsolutePath() + ". Error: " + e.getMessage(), e);
        }

        // Read lines from file
        ArrayList<String> lines;
        try {
            lines = new ArrayList<>(Files.readAllLines(inputFile.toPath()));
        } catch (IOException e) {
            throw new RuntimeException("Failed to read file: " + inputFile.getAbsolutePath() + ". Error: " + e.getMessage(), e);
        }

        // Check if lines are empty
        if (lines.isEmpty()) throw new IllegalArgumentException("File is empty: " + inputFile.getAbsolutePath());
        else loader.load(null, lines);
    }

    private static void manualMode() {

        // Init Scanner and lines
        Scanner scanner = new Scanner(System.in);
        ArrayList<String> lines = new ArrayList<>();

        // Get Inputs
        while (true) {

            // Get Name
            System.out.println("Enter Episode: (empty line to finish)");
            String name = scanner.nextLine();
            if (name.isBlank()) break;
            ArrayList<String> links = new ArrayList<>();

            // Get Parts
            System.out.println("\nEnter URLs (empty line to finish):");
            while (true) {
                String link = scanner.nextLine().trim().replaceAll(" ", "");
                if (!link.isBlank()) links.add(link);
                else break;
            }

            ArrayList<String> uniqueLinks = new ArrayList<>();
            for (String link : links) if (!uniqueLinks.contains(link)) uniqueLinks.add(link);

            // Add to lines
            lines.add(name);
            lines.addAll(uniqueLinks);
        }

        // Close Scanner and process lines
        scanner.close();
        loader.load(null, lines);
    }

    private static void defaultMode () {

        // Init Scanner
        Scanner scanner = new Scanner(System.in);

        // Get Season Format
        System.out.println("Enter Season: (e.g. S01E) or leave empty to skip");
        String seasonFormat = scanner.nextLine().trim();

        // Get Inputs
        System.out.println("\nEnter URLs (3 empty lines to finish):");
        HashSet<String> links = new HashSet<>();
        var i = 0;

        // Get URLs
        while (true) {
            String input = scanner.nextLine().trim().replaceAll(" ", "");
            if (input.isBlank()) {
                i++;
                if (i == 3) break;
            } else {
                i = 0;
                links.add(input);
            }
        }

        // Close Scanner
        scanner.close();

        // Process Links
        if (seasonFormat.isBlank()) loader.load(null, new ArrayList<>(links));
        else loader.load(seasonFormat, new ArrayList<>(links));
    }
}