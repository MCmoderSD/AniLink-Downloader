package de.MCmoderSD.utilities;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.Comparator;
import java.util.HashMap;
import java.util.regex.Pattern;

public class LinkProcessor {

    // Define valid characters for episode numbers
    public static final ArrayList<Character> NUMBERS = new ArrayList<>(Arrays.asList('0', '1', '2', '3', '4', '5', '6', '7', '8', '9'));

    // Determine the common prefix for season format
    public static String determineSeasonFormat(ArrayList<String> links) {

        // Extract filenames from URLs
        var filenames = new ArrayList<String>();
        for (var link : links) {
            var lastSlash = link.lastIndexOf('/');
            if (lastSlash != -1 && lastSlash < link.length() - 1) filenames.add(link.substring(lastSlash + 1));
        }

        // Find the longest common prefix
        var prefix = filenames.getFirst();
        for (var i = 1; i < filenames.size(); i++) {
            while (!filenames.get(i).startsWith(prefix)) {
                prefix = prefix.substring(0, prefix.length() - 1);
                if (prefix.isEmpty()) throw new IllegalArgumentException("No common prefix found in the provided links.");
            }
        }

        return prefix;
    }

    // Process links into episodes based on the determined prefix
    public static HashMap<String, ArrayList<String>> processEpisodes(String prefix, ArrayList<String> links) {

        // Variables
        var episodes = new HashMap<String, ArrayList<String>>();

        // Process links
        for (var link : links) {

            // Filter Season Format
            String episode = link.substring(link.indexOf(prefix) + prefix.length());

            // Number index
            var index = 0;
            while (NUMBERS.contains(episode.charAt(index))) index++;
            episode = episode.substring(0, index);                  // Get number end
            episode = String.valueOf(Integer.parseInt(episode));    // Remove leading zeros

            // Add link to episode
            if (episodes.containsKey(episode)) episodes.get(episode).add(link); // Add part
            else {
                ArrayList<String> list = new ArrayList<>();
                list.add(link);
                episodes.put(episode, list);
            }
        }

        // Sort parts for each episode
        for (var entry : episodes.entrySet()) entry.setValue(sortLinks(entry.getValue()));

        // Return episodes
        return episodes;
    }

    // Sort links by part number
    public static ArrayList<String> sortLinks(ArrayList<String> links) {

        // Skip if there are less than 2 parts
        if (links.size() < 2) return links;

        // Sort parts
        links.sort(Comparator.comparingInt(url -> {
            var matcher = Pattern.compile("part(\\d+)").matcher(url);
            if (matcher.find()) return Integer.parseInt(matcher.group(1));
            return 0;
        }));

        return links;
    }
}
