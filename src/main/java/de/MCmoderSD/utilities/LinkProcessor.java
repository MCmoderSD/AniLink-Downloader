package de.MCmoderSD.utilities;

import java.util.ArrayList;
import java.util.Comparator;
import java.util.HashMap;
import java.util.regex.Pattern;

import static de.MCmoderSD.utilities.Helper.NUMBERS;

public class LinkProcessor {

    public static String determineSeasonFormat(ArrayList<String> links) {

        // Extract filenames from URLs
        ArrayList<String> filenames = new ArrayList<>();
        for (String link : links) {
            var lastSlash = link.lastIndexOf('/');
            if (lastSlash != -1 && lastSlash < link.length() - 1) filenames.add(link.substring(lastSlash + 1));
        }

        // Prefix
        if (filenames.isEmpty()) throw new IllegalArgumentException("No valid filenames found in the provided links.");

        // Find the longest common prefix
        String prefix = filenames.getFirst();
        for (var i = 1; i < filenames.size(); i++) {
            while (!filenames.get(i).startsWith(prefix)) {
                prefix = prefix.substring(0, prefix.length() - 1);
                if (prefix.isEmpty()) throw new IllegalArgumentException("No common prefix found in the provided links.");
            }
        }

        // Return Prefix
        return prefix;
    }

    public static HashMap<String, ArrayList<String>> processLinks(String seasonFormat, ArrayList<String> links) {

        // Variables
        HashMap<String, ArrayList<String>> episodeParts = new HashMap<>();

        // Process links
        for (String link : links) {

            // Filter Season Format
            String episode = link.substring(link.indexOf(seasonFormat) + seasonFormat.length());

            // Number index
            var index = 0;
            while (NUMBERS.contains(episode.charAt(index))) index++;
            episode = episode.substring(0, index);                  // Get number end
            episode = String.valueOf(Integer.parseInt(episode));    // Remove leading zeros

            // Add link to episode
            if (episodeParts.containsKey(episode)) episodeParts.get(episode).add(link); // Add part
            else {
                ArrayList<String> list = new ArrayList<>();
                list.add(link);
                episodeParts.put(episode, list);
            }
        }

        // Sort episode parts
        for (var entry : episodeParts.entrySet()) {

            // Skip if there are less than 2 parts
            if (entry.getValue().size() < 2) continue;

            // Get episode and parts
            var episode = entry.getKey();
            var parts = entry.getValue();

            // Sort parts
            parts.sort(Comparator.comparingInt(url -> {
                var matcher = Pattern.compile("part(\\d+)").matcher(url);
                if (matcher.find()) return Integer.parseInt(matcher.group(1));
                return 0;
            }));

            // Replace entry with sorted parts
            episodeParts.replace(episode, parts);
        }

        // Return episode parts
        return episodeParts;
    }
}