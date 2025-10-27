package de.MCmoderSD.core;

import de.MCmoderSD.debrid.core.DebridAPI;
import de.MCmoderSD.debrid.objects.Download;
import de.MCmoderSD.objects.VideoFile;
import de.MCmoderSD.utilities.SubtitleExtractor;

import java.io.File;
import java.io.IOException;
import java.net.URISyntaxException;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.concurrent.atomic.AtomicInteger;

import static de.MCmoderSD.main.Main.*;
import static de.MCmoderSD.utilities.Helper.holdUp;

public class Downloader {

    // Attributes
    private final DebridAPI debridAPI;
    private final SubtitleExtractor subtitleExtractor;
    private final ArrayList<VideoFile> completed;
    private final ArrayList<VideoFile> failed;

    // Constructor
    public Downloader(String apiKey) {

        // Initialize Debrid API
        debridAPI = new DebridAPI(apiKey);

        // Initialize Subtitle Extractor
        subtitleExtractor = SubtitleExtractor.init(new File(MKV_TOOL_NIX_PATH));

        // Initialize completed and failed lists
        completed = new ArrayList<>();
        failed = new ArrayList<>();
    }

    public void dl(HashMap<String, ArrayList<String>> episodeParts) {

        // Create VideoFiles
        System.out.println("Creating Video Files...\n");
        ArrayList<VideoFile> videoFiles = new ArrayList<>();
        for (var entry : episodeParts.entrySet()) {

            // Get episode and parts
            var episode = entry.getKey();
            var parts = entry.getValue();

            // Create downloads
            ArrayList<Download> downloads = new ArrayList<>();
            for (String link : parts) {
                try {
                    downloads.add(debridAPI.addDownload(link));
                    holdUp(DELAY);
                } catch (IOException | URISyntaxException e) {
                    failed.add(new VideoFile(episode.substring(1), downloads));
                    throw new RuntimeException("Error while creating download for episode " + episode + ": " + e.getMessage(), e);
                }
            }

            // Create VideoFile
            VideoFile videoFile = new VideoFile(episode, downloads);
            videoFiles.add(videoFile);

            // Log the creation of the video file
            System.out.println("Created: " + videoFile.getName() + ".mkv with " + downloads.size() + " parts.");
        }

        // Download archives
        System.out.println("\nStarting downloads...\n");
        AtomicInteger activeThreads = new AtomicInteger(videoFiles.size());
        for (var videoFile : videoFiles) new Thread(() -> {
            try {

                // Download
                videoFile.download();

                // Generate subtitles
                boolean success = videoFile.generateSubs(subtitleExtractor);

                // Check subtitle generation success
                if (!success) failed.add(videoFile);
                else completed.add(videoFile);

                // Log completion
                System.out.println("Completed: " + videoFile.getName() + ".mkv");

            } catch (IOException e) {
                System.err.println("Error: " + e.getMessage());
                failed.add(videoFile);
                activeThreads.decrementAndGet();
                Thread.currentThread().interrupt();
            }
            activeThreads.decrementAndGet();
        }, videoFile.getName() + ".mkv").start();

        // Wait for all threads to finish
        while (activeThreads.get() > 0) holdUp(activeThreads.get());

        // Log completion
        System.out.println("\nFinished downloading all episodes.");
        System.out.println("Completed: " + completed.size() + " | Failed: " + failed.size());
        if (!failed.isEmpty()) {
            System.out.println("Failed episodes:");
            for (var videoFile : failed) System.out.println("- " + videoFile.getName());
        }
    }
}