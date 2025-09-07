package de.MCmoderSD.utilities;

import java.io.File;
import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.zip.CRC32;

import static de.MCmoderSD.main.Main.DEBUG;

@SuppressWarnings("BooleanMethodIsAlwaysInverted")
public class Helper {

    // Constants
    public static final File TEMP_DIR = new File(System.getProperty("java.io.tmpdir"));
    public static final ArrayList<Character> NUMBERS = new ArrayList<>(Arrays.asList('0', '1', '2', '3', '4', '5', '6', '7', '8', '9'));

    // Attributes
    private static final CRC32 crc = new CRC32();

    // Helper methods
    public static void holdUp() {
        holdUp(0);
    }

    public static void holdUp(long millis) {
        try {
            Thread.sleep(millis);
        } catch (InterruptedException e) {
            Thread.currentThread().interrupt();
            throw new RuntimeException("Thread was interrupted while waiting", e);
        }
    }

    public static void log(String message) {
        if (DEBUG) System.out.println("[" + Thread.currentThread().getName() + "] " + message);
    }

    public static String hash(String input) {
        crc.update(input.getBytes(StandardCharsets.UTF_8));
        return Long.toHexString(crc.getValue());
    }

    public static File getWorkingDirectory() {
        try {
            File workingDirectory = new File(".").getCanonicalFile();
            checkDirectory(workingDirectory);
            return workingDirectory;
        } catch (IOException e) {
            throw new RuntimeException("Failed to get working directory", e);
        }
    }

    public static void checkDirectory(File directory) throws IOException {
        if (!directory.exists()) throw new IOException("Directory does not exist: " + directory.getAbsolutePath());
        if (!directory.isDirectory()) throw new IOException("Not a directory: " + directory.getAbsolutePath());
    }

    public static void checkFile(File file) throws IOException {
        if (!file.exists()) throw new IOException("File does not exist: " + file.getAbsolutePath());
        if (!file.isFile()) throw new IOException("Not a file: " + file.getAbsolutePath());
    }

    public static boolean isReadAble(File file) throws IOException {
        checkFile(file);
        return file.canRead();
    }

    public static boolean isWriteAble(File file) throws IOException {
        checkFile(file);
        return file.canWrite();
    }

    public static boolean isExecutable(File file) throws IOException {
        checkFile(file);
        return file.canExecute();
    }

    public static void deleteFile(File file) throws IOException {
        if (file.exists() && file.isDirectory()) deleteDirectory(file);
        else if (file.exists() && !file.delete()) throw new IOException("Could not delete file: " + file.getAbsolutePath());
    }

    public static void deleteDirectory(File directory) throws IOException {
        if (directory.exists() && directory.isFile()) deleteFile(directory);
        else if (directory.exists() && directory.isDirectory()) {
            var contents = directory.listFiles();
            if (contents != null && contents.length > 0) throw new IOException("Directory is not empty: " + directory.getAbsolutePath());
            if (!directory.delete()) throw new IOException("Could not delete directory: " + directory.getAbsolutePath());
        }
    }

    @SuppressWarnings("SameReturnValue")
    public static boolean moveFile(File source, File destination) throws IOException {
        if (source.getAbsolutePath().equals(destination.getAbsolutePath())) return true;
        checkFile(source);
        deleteFile(destination);
        if (!source.renameTo(destination)) throw new IOException("Could not move file from " + source.getAbsolutePath() + " to " + destination.getAbsolutePath());
        return true;
    }
}