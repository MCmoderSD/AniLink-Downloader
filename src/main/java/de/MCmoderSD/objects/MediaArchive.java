package de.MCmoderSD.objects;

import de.MCmoderSD.debrid.objects.Download;
import de.MCmoderSD.enums.MediaType;
import de.MCmoderSD.utilities.ArchiveProcessor;

import java.io.*;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.*;
import java.util.concurrent.atomic.AtomicInteger;

import static de.MCmoderSD.main.Main.PASSWORD;

@SuppressWarnings("ResultOfMethodCallIgnored")
public class MediaArchive {

    // Static
    public static final AtomicInteger ARCHIVE_COUNTER = new AtomicInteger(0);

    // Attributes
    private final File path;
    private final String name;
    private final Download[] parts;
    private final File partsDir;
    private final File outputDir;

    // Variable
    private MediaType mediaType;

    // Constructor
    public MediaArchive(String name, Download[] parts) {

        // Set Attributes
        this.parts = parts;
        this.name = sanitizeName(name);
        this.path = getArchivePath(name + "_" + ARCHIVE_COUNTER.incrementAndGet() + "-" + ProcessHandle.current().pid());
        this.partsDir = new File(path, "archives");
        this.outputDir = new File(path, "output");

        // Create directories
        if (!path.mkdirs()) throw new RuntimeException("Failed to create archive directory: " + path.getAbsolutePath());
        if (!partsDir.mkdirs()) throw new RuntimeException("Failed to create archive directory: " + partsDir.getAbsolutePath());
        if (!outputDir.mkdirs()) throw new RuntimeException("Failed to create archive directory: " + outputDir.getAbsolutePath());
    }

    // Helper Methods
    private static String sanitizeName(String name) {
        return name.replaceAll("[\\\\/:*?\"<>|]", "-").trim();
    }

    private static File getArchivePath(String name) {
        return new File(System.getProperty("java.io.tmpdir"), sanitizeName(name));
    }

    // Methods
    public void downloadParts() {
        for (var part : parts) {
            try {
                part.toFile(new File(partsDir, part.getName()));
            } catch (Exception e) {
                throw new RuntimeException("Failed to download part: " + part.getDownloadLink(), e);
            }
        }
    }

    public void extractMedia() {
        try {
            var firstPart = new File(partsDir, parts[0].getName());
            ArchiveProcessor.getInstance().extract(firstPart, outputDir, PASSWORD);
        }  catch (Exception e) {
            throw new RuntimeException("Failed to extract media archive: " + e.getMessage(), e);
        }

        // Validate extracted files
        var outFiles = outputDir.listFiles();
        if (outFiles == null || outFiles.length == 0) throw new RuntimeException("No files found in the extracted archive: " + outputDir.getAbsolutePath());
        if (outFiles.length > 1) throw new RuntimeException("Multiple files found in the extracted archive. Expected only one media file: " + outputDir.getAbsolutePath());
        mediaType = MediaType.getMediaType(outFiles[0].getName().substring(outFiles[0].getName().lastIndexOf('.')));

        // Rename the extracted file to match the original name
        var success = outFiles[0].renameTo(new File(path, name + mediaType.getExtension()));
        if (!success) throw new RuntimeException("Failed to rename media archive: " + outFiles[0].getAbsolutePath());
    }

    public MediaFile moveMediaFile(File directory) {

        // Initialize files
        var mediaFile = new File(path, name + mediaType.getExtension());
        var targetFile = new File(directory, mediaFile.getName());

        // Move the media file to the target directory
        if (!mediaFile.renameTo(targetFile)) throw new RuntimeException("Failed to move media file to target directory: " + targetFile.getAbsolutePath());

        // Return the media file object
        return new MediaFile(targetFile);
    }

    public void cleanUp() {
        try (var stream = Files.walk(path.toPath())) {
            stream.sorted(Comparator.reverseOrder())
                    .map(Path::toFile)
                    .forEach(File::delete);
        } catch (IOException e) {
            throw new RuntimeException("Failed to clean up temporary files: " + e.getMessage(), e);
        }
    }
}