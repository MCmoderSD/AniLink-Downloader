package de.MCmoderSD.objects;

import de.MCmoderSD.debrid.objects.Download;
import de.MCmoderSD.utilities.SubtitleExtractor;

import java.io.File;
import java.io.IOException;
import java.util.ArrayList;

import static de.MCmoderSD.utilities.DirectoryCleaner.cleanUpDirectory;
import static de.MCmoderSD.utilities.Helper.*;

public class VideoFile {

    // Attributes
    private final String name;
    private final RarArchive rarArchive;

    // Variable
    private File mkvFile;

    // Constructor
    public VideoFile(String name, ArrayList<Download> downloads) {
        this.name = name.replaceAll("[^a-zA-Z0-9]", "-");
        this.rarArchive = new RarArchive(downloads);
    }

    // Methods
    public void download() throws IOException {

        // Download and extract MKV file
        mkvFile = rarArchive.downloadAndExtractMKV();
        if (!isReadAble(mkvFile)) throw new IOException("Downloaded MKV file is not readable: " + mkvFile.getAbsolutePath());
        log("Downloaded and extracted MKV file: " + mkvFile.getAbsolutePath());

        // Create folder in current directory
        File folder = new File(name);
        if (folder.exists()) {
            cleanUpDirectory(folder);
            if (!folder.delete()) throw new IOException("Failed to delete existing folder: " + folder.getAbsolutePath());
        }
        if (!folder.mkdirs()) throw new IOException("Failed to create folder: " + folder.getAbsolutePath());
        checkDirectory(folder);
        log("Created folder: " + folder.getAbsolutePath());

        // Move MKV file to the new folder
        File rarArchive = mkvFile.getParentFile();
        File renamedFile = new File(folder, name + ".mkv");
        if (!moveFile(mkvFile, renamedFile)) throw new IOException("Failed to move MKV file to folder: " + folder.getAbsolutePath());
        mkvFile = renamedFile;

        // Delete Temporary RAR files
        cleanUpDirectory(rarArchive);
    }

    public boolean generateSubs(SubtitleExtractor subtitleExtractor) {
        try {

            // Extract subtitles
            subtitleExtractor.extract(mkvFile);

            return true;
        } catch (IOException | InterruptedException e) {
            System.out.println("Failed to extract subtitles: " + e.getMessage());
            return false;
        }
    }

    // Getters
    public String getName() {
        return name;
    }
}