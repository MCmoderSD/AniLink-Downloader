import java.io.*;
import java.net.URI;
import java.net.URISyntaxException;
import java.util.ArrayList;
import java.util.Scanner;

import static de.MCmoderSD.utilities.Helper.getWorkingDirectory;

@SuppressWarnings("unused")
public class Test {

    private static void downloader() {
        ArrayList<String> lines = new ArrayList<>();
        Scanner scanner = new Scanner(System.in);
        while (true) {
            String line = scanner.nextLine();
            if (line.isBlank()) break;
            lines.add(line);
        }
        scanner.close();
        for (String line : lines) download(line);
    }

    public static void download(String url) {
        new Thread(() -> {
            File file = new File(url.substring(url.lastIndexOf('/') + 1));
            try (
                    BufferedInputStream bufferedInputStream = new BufferedInputStream(new URI(url).toURL().openStream());
                    BufferedOutputStream bufferedOutputStream = new BufferedOutputStream(new FileOutputStream(file))
            ) {
                bufferedInputStream.transferTo(bufferedOutputStream);
            } catch (IOException | URISyntaxException e) {
                throw new RuntimeException(e);
            }
            System.out.println("Downloaded: " + file.getAbsolutePath());
        }).start();
    }

    public static void rename() {
        File dir = getWorkingDirectory();
        File[] files = dir.listFiles((d, name) -> name.endsWith(".mkv"));
        if (files == null) return;
        for (var file : files) {
            String fileName = file.getName();
            if (fileName.contains("S04") && fileName.contains(".German")) {
                String newFileName = fileName.substring(fileName.lastIndexOf("S04") + 4, fileName.lastIndexOf(".German")) + ".mkv";
                while (newFileName.startsWith("0")) newFileName = newFileName.substring(1);
                File newFile = new File(dir, newFileName);
                if (file.renameTo(newFile)) System.out.println("Renamed: " + file.getAbsolutePath() + " -> " + newFile.getAbsolutePath());
                else System.out.println("Failed to rename: " + file.getAbsolutePath());
            }
        }
    }

    public static void renameSub() {
        File dir = getWorkingDirectory();
        File[] files = dir.listFiles((d, name) -> name.endsWith(".srt"));
        if (files == null) return;
        for (var file : files) {
            String fileName = file.getName();
            if (fileName.contains("_ger")) {
                String newFileName = fileName.substring(0, fileName.indexOf("_subtitle")) + ".de.srt";
                while (newFileName.startsWith("0")) newFileName = newFileName.substring(1);
                File newFile = new File(dir, newFileName);
                if (file.renameTo(newFile)) System.out.println("Renamed: " + file.getAbsolutePath() + " -> " + newFile.getAbsolutePath());
                else System.out.println("Failed to rename: " + file.getAbsolutePath());
            }
        }
    }
}