package de.MCmoderSD.core;

import de.MCmoderSD.debrid.core.DebridAPI;
import de.MCmoderSD.debrid.objects.Download;
import de.MCmoderSD.objects.MediaArchive;
import de.MCmoderSD.objects.MediaFile;
import org.jetbrains.annotations.Nullable;

import java.io.File;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.HashSet;

import static de.MCmoderSD.main.Main.DELAY;
import static de.MCmoderSD.utilities.LinkProcessor.*;

@SuppressWarnings("BusyWait")
public class InputParser {

    // Attributes
    private final File workDir;
    private final DebridAPI debridAPI;

    // Constructor
    public InputParser(File workDir, String apiKey) {
        this.workDir = workDir;
        this.debridAPI = new DebridAPI(apiKey);
    }

    // Methods
    public void loadSeason(@Nullable String prefix, ArrayList<String> lines) {

        // Process Lines
        lines.replaceAll(String::trim);
        lines.removeIf(String::isBlank);

        // Check if Prefix is present
        if (prefix == null || prefix.isBlank()) {
            prefix = determineSeasonFormat(lines);
            IO.println("Determined Season Format: " + prefix + "\n");
        } else IO.println("Using provided Season Format: " + prefix + "\n");

        // Process Links
        HashMap<String, ArrayList<String>> episodeParts = processEpisodes(prefix, lines);
        IO.println("Processed " + lines.size() + " links into " + episodeParts.size() + " episodes.\n");

        // Create Media Archives
        ArrayList<MediaArchive> mediaArchives = new ArrayList<>();

        // Process episodes
        for (var episode : episodeParts.entrySet()) {

            // Get episode and links
            var name = episode.getKey();
            var links = episode.getValue();
            var downloads = new Download[links.size()];

            // Process links for episode
            for (int i = 0; i < links.size(); i++) {
                try {
                    downloads[i] = debridAPI.addDownload(links.get(i));
                    Thread.sleep(DELAY);
                } catch (Exception e) {
                    throw new RuntimeException("Error while creating download for episode " + name + ": " + e.getMessage(), e);
                }
            }

            // Create MediaArchive for episode
            mediaArchives.add(new MediaArchive(name, downloads));
            IO.println("Created Episode " + name);
        }

        // Create Media Files
        ArrayList<MediaFile> mediaFiles = new ArrayList<>();

        // Create threads for each episode
        ArrayList<Thread> threads = new ArrayList<>();
        for (var archive : mediaArchives) threads.add(new Thread(() -> {

            // Download & Extract
            archive.downloadParts();
            archive.extractMedia();

            // Move media file to work directory
            var mediaFile = archive.moveMediaFile(workDir);
            mediaFiles.add(mediaFile);

            // Clean Up
            archive.cleanUp();
            IO.println("Completed: " + mediaFile.getFile().getName());
        }));

        // Start threads
        for (var thread : threads) thread.start();
        IO.println("\nStarting Downloads & Extraction:\n");

        // Wait for threads to finish
        while (threads.stream().anyMatch(Thread::isAlive)) {
            try {
                Thread.sleep(DELAY);
            } catch (InterruptedException e) {
                Thread.currentThread().interrupt();
                throw new RuntimeException("Thread interrupted while waiting for downloads to complete.", e);
            }
        }

        // Log completion
        IO.println("\nDownloaded " + mediaFiles.size() + " of " + mediaArchives.size() + " episodes.");
    }

    public void loadFile(HashMap<String, HashSet<String>> files) {

        // Create Media Archives
        ArrayList<MediaArchive> mediaArchives = new ArrayList<>();

        // Process files
        IO.println("Processing files\n");
        for (var file : files.entrySet()) {

            // Init Variables
            var name = file.getKey();
            var downloads = new Download[file.getValue().size()];

            // Process links
            ArrayList<String> links = new ArrayList<>();
            for (var link : file.getValue()) links.add(link.trim());
            links.removeIf(String::isBlank);
            sortLinks(links);

            // Process links for file
            for (int i = 0; i < links.size(); i++) {
                try {
                    downloads[i] = debridAPI.addDownload(links.get(i));
                    Thread.sleep(DELAY);
                } catch (Exception e) {
                    throw new RuntimeException("Error while creating download for File " + name + ": " + e.getMessage(), e);
                }
            }

            // Create MediaArchive for file
            mediaArchives.add(new MediaArchive(name, downloads));
            IO.println("Created File " + name);
        }

        // Create Media Files
        ArrayList<MediaFile> mediaFiles = new ArrayList<>();

        // Create threads for each file
        ArrayList<Thread> threads = new ArrayList<>();
        for (var archive : mediaArchives) threads.add(new Thread(() -> {

            // Download & Extract
            archive.downloadParts();
            archive.extractMedia();

            // Move media file to work directory
            var mediaFile = archive.moveMediaFile(workDir);
            mediaFiles.add(mediaFile);

            // Clean Up
            archive.cleanUp();
            IO.println("Completed: " + mediaFile.getFile().getName());
        }));

        // Start threads
        for (var thread : threads) thread.start();
        IO.println("\nStarting Downloads & Extraction:\n");

        // Wait for threads to finish
        while (threads.stream().anyMatch(Thread::isAlive)) {
            try {
                Thread.sleep(DELAY);
            } catch (InterruptedException e) {
                Thread.currentThread().interrupt();
                throw new RuntimeException("Thread interrupted while waiting for downloads to complete.", e);
            }
        }

        // Log completion
        IO.println("\nDownloaded " + mediaFiles.size() + " of " + mediaArchives.size() + " files.");
    }
}