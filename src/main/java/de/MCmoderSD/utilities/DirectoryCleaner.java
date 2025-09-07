package de.MCmoderSD.utilities;

import java.io.File;
import java.io.IOException;
import java.util.Objects;

import static de.MCmoderSD.utilities.Helper.*;

public class DirectoryCleaner {

    public static void moveFilesRecursively(File directory) throws IOException {
        moveFilesRecursively(directory, directory);
    }

    private static void moveFilesRecursively(File directory, File rootDirectory) throws IOException {
        checkDirectory(directory);
        File[] files = directory.listFiles();
        if (files == null) return;
        for (var file : files) {
            if (file.isDirectory()) moveFilesRecursively(file, rootDirectory);
            else if (!file.getParentFile().equals(rootDirectory)) {
                File newFile = new File(rootDirectory, file.getName());
                if (newFile.exists()) System.out.println("File already exists: " + newFile.getAbsolutePath());
                else {
                    if (moveFile(file, newFile)) System.out.println("Moved file: " + file.getAbsolutePath() + " to " + newFile.getAbsolutePath());
                    else System.out.println("Failed to move file: " + file.getAbsolutePath());
                }
            }
        }
    }

    public static void deleteEmptyDirectory(File directory) throws IOException {
        deleteEmptyDirectory(directory, directory);
    }

    private static void deleteEmptyDirectory(File directory, File rootDirectory) throws IOException {
        checkDirectory(directory);
        File[] directories = directory.listFiles(File::isDirectory);
        if (directories == null) return;
        for (var subDirectory : directories) {
            deleteEmptyDirectory(subDirectory, rootDirectory);
            if (!subDirectory.equals(rootDirectory) && Objects.requireNonNull(subDirectory.list()).length == 0) deleteDirectory(subDirectory);
        }
    }

    public static void cleanUpDirectory(File directory) throws IOException {
        if (directory == null || !directory.isDirectory()) throw new IllegalArgumentException("Provided path is not a valid directory: " + directory);
        for (var file : Objects.requireNonNull(directory.listFiles())) {
            if (file.isDirectory()) {
                cleanUpDirectory(file); // Recursively delete contents
                deleteFile(file);
            } else deleteFile(file);
        }
    }
}