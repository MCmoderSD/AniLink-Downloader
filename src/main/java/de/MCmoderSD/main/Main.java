package de.MCmoderSD.main;

import de.MCmoderSD.core.InputParser;
import de.MCmoderSD.utilities.ConfigProcessor;

import java.io.File;
import java.io.IOException;
import java.nio.file.Files;
import java.util.*;

public class Main {

    // Version
    public static final String VERSION = "2.0.1";

    // Config
    public static long DELAY;
    public static String PASSWORD;
    public static String API_KEY;

    // Main Method
    static void main(String[] args) {
        IO.println("AniLink-Downloader v" + VERSION + "\n");

        // Init Config
        var config = new ConfigProcessor();
        DELAY = config.getDelay();
        PASSWORD = config.getPassword();
        API_KEY = config.getApiKey();

        // Get Working Directory
        var workDir = new File(System.getProperty("user.dir"));

        // Initialize InputParser
        var inputParser = new InputParser(workDir, API_KEY);

        // Parse Arguments
        var arguments = new ArrayList<>(Arrays.asList(args));

        // Check for modes
        switch (!arguments.isEmpty() ? arguments.getFirst().toLowerCase() : "") {
            case "--manual", "-m" -> manualMode(inputParser);
            case "--movie", "-mo" -> movieMode(workDir, inputParser);
            case "--input", "-i" -> {
                if (arguments.size() == 2) inputMode(arguments.get(1), inputParser);
                else throw new IllegalArgumentException("Input mode requires a file path argument. Usage: --input <file_path>");
            }
            case "--version", "-v" -> {
                IO.println("AniLink-Downloader v" + VERSION + "\n");
                System.exit(0);
            }
            default -> defaultMode(inputParser);
        }
    }

    // Modes
    private static void defaultMode(InputParser inputParser) {

        // Get Season Format
        var seasonFormat = IO.readln("Enter Season: (e.g. S01E) or leave empty to skip\n").trim();

        // Get Inputs
        IO.println("\nEnter URLs (3 empty lines to finish):");
        var links = new HashSet<String>();
        var i = 0;

        // Get URLs
        while (true) {
            var input = IO.readln().trim();
            if (input.isBlank()) {
                i++;
                if (i == 3) break;
            } else {
                i = 0;
                links.add(input);
            }
        }

        // Parse Inputs
        inputParser.loadSeason(seasonFormat, new ArrayList<>(links));
    }

    private static void manualMode(InputParser inputParser) {

        // Init Variables
        var files = new HashMap<String, HashSet<String>>();

        // Get Inputs
        while (true) {

            // Get Name
            var name = IO.readln("Enter Episode: (empty line to finish)\n").trim();
            if (name.isBlank()) break;

            // Get Parts
            var links = new ArrayList<String>();
            IO.println("\nEnter URLs (empty line to finish):");
            while (true) {
                var link = IO.readln().trim();
                if (link.isBlank()) break;
                links.add(link);
            }

            // Add to files
            files.put(name, new HashSet<>(links));
        }

        // Parse Inputs
        inputParser.loadFile(files);
    }

    private static void movieMode(File workDir, InputParser inputParser) {

        // Init Variables
        var movies = new HashMap<String, HashSet<String>>();
        var directories = new ArrayList<>(List.of(Objects.requireNonNull(workDir.listFiles())));

        // Process Directories
        for (var directory : directories) {

            // Skip non-directories
            if (!directory.isDirectory()) continue;

            // Get name and files
            var name = directory.getName().trim();
            var files = directory.listFiles();

            // Skip if there are no files in the directory
            if (files == null || files.length == 0) continue;

            // Read lines from all txt files in the directory
            var lines = new ArrayList<String>();
            for (var file : files) {

                // Filter out non-txt files
                if (!file.isFile() || !file.getName().toLowerCase().endsWith(".txt")) continue;
                try {
                    Files.readAllLines(file.toPath()).stream().map(String::trim).filter(line -> !line.isBlank()).forEach(lines::add);
                } catch (IOException e) {
                    throw new RuntimeException("Failed to read file: " + file.getAbsolutePath(), e);
                }
            }

            // Remove duplicates and trim whitespace
            var links = new HashSet<String>();
            for (var line : lines) links.add(line.trim());

            // Add to movies
            movies.put(name, links);

            // Log Movie
            IO.println("Added Movie: " + name + " with " + links.size() + " links");
        }

        // Parse Inputs
        inputParser.loadFile(movies);
    }

    private static void inputMode(String path, InputParser inputParser) {

        // Read lines from the specified file
        var lines = new ArrayList<String>();
        try {
            Files.readAllLines(new File(path).toPath()).stream().map(String::trim).filter(line -> !line.isBlank()).forEach(lines::add);
        } catch (IOException e) {
            throw new RuntimeException("Failed to read file: " + path, e);
        }

        // Remove duplicates and trim whitespace
        var links = new HashSet<String>();
        for (var line : lines) links.add(line.trim());

        // Parse Inputs
        inputParser.loadSeason(null, new ArrayList<>(links));
    }
}