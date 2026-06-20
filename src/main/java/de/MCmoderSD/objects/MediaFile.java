package de.MCmoderSD.objects;

import de.MCmoderSD.enums.MediaType;

import java.io.File;

@SuppressWarnings("unused")
public class MediaFile {

    // Attributes
    private final File file;
    private final String name;
    private final MediaType mediaType;
    private final MediaProbe mediaProbe;

    // Constructor
    public MediaFile(File file) {
        this.file = file;
        this.name = file.getName().substring(0, file.getName().lastIndexOf('.'));
        this.mediaType = MediaType.getMediaType(file.getName().substring(file.getName().lastIndexOf('.')));
        this.mediaProbe = new MediaProbe(file);
    }

    // Getter
    public File getFile() {
        return file;
    }

    public String getName() {
        return name;
    }

    public MediaType getMediaType() {
        return mediaType;
    }

    public String getExtension() {
        return mediaType.getExtension();
    }

    public MediaProbe getMediaProbe() {
        return mediaProbe;
    }
}