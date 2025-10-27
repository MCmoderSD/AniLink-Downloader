package de.MCmoderSD.core;

import org.jetbrains.annotations.Nullable;

import java.util.ArrayList;
import java.util.HashMap;

import static de.MCmoderSD.utilities.LinkProcessor.*;

@SuppressWarnings("ClassCanBeRecord")
public class Loader {

    // Attributes
    private final Downloader downloader;

    // Constructor
    public Loader(String apiKey) {
        downloader = new Downloader(apiKey);
    }

    // Methods
    public void load(@Nullable String seasonFormat, ArrayList<String> lines) {

        // Filter out empty lines and trim whitespace
        lines.replaceAll(String::trim);
        lines.removeIf(String::isBlank);

        // Check if all files start with "http"
        boolean allStartWithHttp = lines.stream().allMatch(line -> line.startsWith("http"));
        if (allStartWithHttp) autoLoad(seasonFormat, lines);
        else manuLoad(lines);
    }

    private void autoLoad(@Nullable String seasonFormat, ArrayList<String> lines) {

        if (seasonFormat == null || seasonFormat.isBlank()) {
            seasonFormat = determineSeasonFormat(lines);
            System.out.println("Detected Season Format: " + seasonFormat);
        }

        // Process Links
        HashMap<String, ArrayList<String>> episodeParts = processLinks(seasonFormat, lines);
        System.out.println("Processed " + lines.size() + " links into " + episodeParts.size() + " episodes.\n");

        // Download episodes
        downloader.dl(episodeParts);
    }

    private void manuLoad(ArrayList<String> lines) {

        // Process lines into episodes and their parts
        HashMap<String, ArrayList<String>> episodeParts = new HashMap<>();
        ArrayList<String> currentDownloads = new ArrayList<>();
        String currentEpisode = null;
        for (String line : lines) {
            if (!line.startsWith("http")) {
                if (currentEpisode != null) episodeParts.put(currentEpisode, currentDownloads); // Save previous episode
                currentEpisode = line.trim().replaceAll(" ", "-");
                currentDownloads = new ArrayList<>();
            } else currentDownloads.add(line);
        }
        if (currentEpisode != null) episodeParts.put(currentEpisode, currentDownloads); // Save last episode
        System.out.println("Processed " + lines.size() + " links into " + episodeParts.size() + " episodes.\n");

        // Download episodes
        downloader.dl(episodeParts);
    }
}