package de.MCmoderSD.main;

import de.MCmoderSD.core.InputParser;
import de.MCmoderSD.json.JsonUtility;

import java.io.File;
import java.io.IOException;
import java.nio.file.Files;
import java.util.*;

public class Main {

    // Version
    public static final String VERSION = "1.1.0";

    // Constants
    public static final long DELAY = 250;                           // Recommended 250ms
    public static final String PASSWORD = "www.anime-loads.org";    // Default Anime-Loads.org Password

    // Main Method
    static void main(String[] args) {
        IO.println("AniLink-Downloader v" + VERSION);

        // Load Config
        var config = JsonUtility.getInstance().loadResource("/config.json");
        var apiKey = config.get("apiKey").asString();

        // Get Working Directory
        var workDir = new File(System.getProperty("user.dir"));

        // Initialize InputParser
        var inputParser = new InputParser(workDir, apiKey);

        // Parse Arguments
        ArrayList<String> arguments = new ArrayList<>(Arrays.asList(args));

        // Check for modes
        switch (!arguments.isEmpty() ? arguments.getFirst().toLowerCase() : "") {
            case "--manual", "-m" -> manualMode(inputParser);
            case "--movie", "-mo" -> movieMode(workDir, inputParser);
            case "--input", "-i" -> {
                if (arguments.size() == 2) inputMode(arguments.get(1), inputParser);
                else throw new IllegalArgumentException("Input mode requires a file path argument. Usage: --input <file_path>");
            }
            default -> defaultMode(inputParser);
        }
    }

    // Modes
    private static void defaultMode(InputParser inputParser) {

        // Init Scanner
        Scanner scanner = new Scanner(System.in);

        // Get Season Format
        IO.println("Enter Season: (e.g. S01E) or leave empty to skip");
        String seasonFormat = scanner.nextLine().trim();

        // Get Inputs
        IO.println("\nEnter URLs (3 empty lines to finish):");
        HashSet<String> links = new HashSet<>();
        var i = 0;

        // Get URLs
        while (true) {
            String input = scanner.nextLine().trim();
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

        // Parse Inputs
        inputParser.loadSeason(seasonFormat, new ArrayList<>(links));
    }

    private static void manualMode(InputParser inputParser) {

        // Init Scanner and lines
        Scanner scanner = new Scanner(System.in);
        HashMap<String, HashSet<String>> files = new HashMap<>();

        // Get Inputs
        while (true) {

            // Get Name
            IO.println("Enter Episode: (empty line to finish)");
            String name = scanner.nextLine().trim();
            if (name.isBlank()) break;

            // Get Parts
            ArrayList<String> links = new ArrayList<>();
            IO.println("\nEnter URLs (empty line to finish):");
            while (true) {
                String link = scanner.nextLine().trim();
                if (link.isBlank()) break;
                links.add(link);
            }

            // Add to files
            files.put(name, new HashSet<>(links));
        }

        // Close Scanner
        scanner.close();

        // Parse Inputs
        inputParser.loadFile(files);
    }

    private static void movieMode(File workDir, InputParser inputParser) {

        // Init Variables
        HashMap<String, HashSet<String>> movies = new HashMap<>();
        ArrayList<File> directories = new ArrayList<>(List.of(Objects.requireNonNull(workDir.listFiles())));

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
            ArrayList<String> lines = new ArrayList<>();
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
            HashSet<String> links = new HashSet<>();
            for (var line : lines) links.add(line.trim());

            // Add to movies
            movies.put(name, links);
        }

        // Parse Inputs
        inputParser.loadFile(movies);
    }

    private static void inputMode(String path, InputParser inputParser) {

        // Read lines from the specified file
        ArrayList<String> lines = new ArrayList<>();
        try {
            Files.readAllLines(new File(path).toPath()).stream().map(String::trim).filter(line -> !line.isBlank()).forEach(lines::add);
        } catch (IOException e) {
            throw new RuntimeException("Failed to read file: " + path, e);
        }

        // Remove duplicates and trim whitespace
        HashSet<String> links = new HashSet<>();
        for (var line : lines) links.add(line.trim());

        // Parse Inputs
        inputParser.loadSeason(null, new ArrayList<>(links));
    }
}