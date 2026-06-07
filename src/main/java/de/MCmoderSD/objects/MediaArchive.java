package de.MCmoderSD.objects;

import com.github.junrar.Archive;
import com.github.junrar.exception.CrcErrorException;
import com.github.junrar.exception.RarException;
import de.MCmoderSD.debrid.objects.Download;
import de.MCmoderSD.enums.MediaType;

import java.io.*;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.Comparator;
import java.util.UUID;

import static de.MCmoderSD.main.Main.PASSWORD;

@SuppressWarnings("ResultOfMethodCallIgnored")
public class MediaArchive {

    // Attributes
    private final File path;
    private final String name;
    private final Download[] parts;
    private final File partsDir;

    // Variable
    private MediaType mediaType;

    // Constructor
    public MediaArchive(String name, Download[] parts) {

        // Set Attributes
        this.parts = parts;
        this.name = sanitizeName(name);
        this.path = getArchivePath(name + "_" + UUID.randomUUID());
        this.partsDir = new File(path, "archives");

        // Create directories
        if (!path.mkdirs()) throw new RuntimeException("Failed to create archive directory: " + path.getAbsolutePath());
        if (!partsDir.mkdirs()) throw new RuntimeException("Failed to create archive directory: " + partsDir.getAbsolutePath());
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
        var firstPart = new File(partsDir, parts[0].getName());
        try (var archive = new Archive(firstPart, PASSWORD)) {

            // Check if the archive contains more than one file
            if (archive.getFileHeaders().size() > 1) {
                System.err.println("Warning: More than one file found in the archive: " + name);
                for (var header : archive.getFileHeaders()) System.err.println(" - " + header.getFileName());
                throw new RuntimeException("Multiple files found in the archive. Expected only one media file.");
            }

            // Extract media file
            var header = archive.getFileHeaders().getFirst();
            mediaType = MediaType.getMediaType(header.getFileName().substring(header.getFileName().lastIndexOf('.')));

            try (var out = new BufferedOutputStream(new FileOutputStream(new File(path, name + mediaType.getExtension())))) {
                archive.extractFile(header, out);
            } catch (RarException e) {
                if (!(e instanceof CrcErrorException)) throw new RuntimeException("Failed to extract media file: " + e.getMessage(), e);
            }

        } catch (RarException | IOException e) {
            throw new RuntimeException("Failed to process RAR archive: " + e.getMessage(), e);
        }
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