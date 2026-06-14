package de.MCmoderSD.utilities;

import net.sf.sevenzipjbinding.*;
import net.sf.sevenzipjbinding.impl.RandomAccessFileInStream;
import net.sf.sevenzipjbinding.simple.ISimpleInArchiveItem;
import java.io.*;
import java.nio.file.Files;
import java.util.LinkedHashMap;

public class ArchiveProcessor {

    // Singleton
    private static ArchiveProcessor instance;

    // Constructor
    private ArchiveProcessor() {
        try {
            SevenZip.initSevenZipFromPlatformJAR();
        } catch (SevenZipNativeInitializationException e) {
            throw new RuntimeException("Failed to initialize 7-Zip native library", e);
        }
    }

    // Singleton Getter
    public static ArchiveProcessor getInstance() {
        if (instance == null) instance = new ArchiveProcessor();
        return instance;
    }

    // Methods
    public void extract(File archive, File outputDir) throws IOException {
        extract(archive, outputDir, null);
    }

    public void extract(File archive, File outputDir, String password) throws IOException {

        // Validate inputs
        if (!archive.exists() || !archive.isFile()) throw new FileNotFoundException("Archive not found: " + archive.getAbsolutePath());
        if (!(outputDir.exists() || outputDir.mkdirs()) || !outputDir.isDirectory()) throw new IOException("Output directory not found: " + outputDir.getAbsolutePath());

        // Initialize Archive and extract items
        var callback = new VolumeCallback(archive, password);
        try (var inArchive = SevenZip.openInArchive(null, callback.getFirstStream(), callback)) {
            var simple = inArchive.getSimpleInterface();
            for (var item : simple.getArchiveItems()) {
                if (item.isFolder()) continue;
                extractItem(item, outputDir, password);
            }
        } catch (SevenZipException e) {
            throw new IOException("Extraction failed: " + e.getMessage(), e);
        } finally {
            callback.closeAll();
        }
    }

    // Helper Methods
    private static void extractItem(ISimpleInArchiveItem item, File outputDir, String password) throws IOException {

        // Sanitize and validate path to prevent path traversal
        String path = item.getPath().replace("\\", File.separator);
        File outFile = new File(outputDir, path);

        // Ensure the output file is within the output directory
        if (!outFile.getCanonicalPath().startsWith(outputDir.getCanonicalPath() + File.separator)) throw new IOException("Path traversal attempt blocked: " + path);
        if (outFile.getParentFile() != null) Files.createDirectories(outFile.getParentFile().toPath());

        // Extract item
        try (var out = new BufferedOutputStream(new FileOutputStream(outFile))) {
            var result = item.extractSlow(data -> writeChunk(out, data), password);
            if (result != ExtractOperationResult.OK) throw new IOException("Failed to extract item '" + path + "': " + result);
        }
    }

    private static int writeChunk(OutputStream out, byte[] data) {
        try {
            out.write(data);
            return data.length;
        } catch (IOException e) {
            return 0;
        }
    }

    // Callback Class
    private static class VolumeCallback implements IArchiveOpenVolumeCallback, IArchiveOpenCallback, ICryptoGetTextPassword {

        // Attributes
        private final File baseDir;
        private final String firstName;
        private final String password;
        private final IInStream firstStream;
        private final LinkedHashMap<String, RandomAccessFile> openFiles;
        private final LinkedHashMap<String, IInStream> openStreams;

        // Constructor
        VolumeCallback(File firstVolume, String password) throws IOException {

            // Set Attributes
            this.baseDir = firstVolume.getParentFile();
            this.firstName = firstVolume.getName();
            this.password = password;

            // Initialize LinkedHashMaps
            openFiles = new LinkedHashMap<>();
            openStreams = new LinkedHashMap<>();

            // Open first volume
            var raf = new RandomAccessFile(firstVolume, "r");
            openFiles.put(firstName, raf);

            // Create stream for first volume
            var stream = new RandomAccessFileInStream(raf);
            openStreams.put(firstName, stream);

            // Set first stream
            this.firstStream = stream;
        }

        public IInStream getFirstStream() {
            return firstStream;
        }

        @Override
        public IInStream getStream(String filename) {
            if (openStreams.containsKey(filename)) return openStreams.get(filename);

            File volumeFile = new File(baseDir, filename);
            if (!volumeFile.exists()) return null;

            try {
                var raf = new RandomAccessFile(volumeFile, "r");
                openFiles.put(filename, raf);
                var stream = new RandomAccessFileInStream(raf);
                openStreams.put(filename, stream);
                return stream;
            } catch (FileNotFoundException e) {
                return null;
            }
        }

        @Override
        public Object getProperty(PropID propID) {
            return propID == PropID.NAME ? firstName : null;
        }

        @Override
        public String cryptoGetTextPassword() { return password != null ? password : ""; }

        @Override public void setCompleted(Long files, Long bytes) {}
        @Override public void setTotal(Long files, Long bytes) {}

        void closeAll() {
            openFiles.values().forEach(raf -> {
                try { raf.close(); } catch (IOException ignored) {}
            });
            openFiles.clear();
            openStreams.clear();
        }
    }
}