package de.MCmoderSD.enums;

public enum MediaType {

    // Enum Constants
    MKV, MP4;

    // Getters
    public String getExtension() {
        return switch (this) {
            case MKV -> ".mkv";
            case MP4 -> ".mp4";
        };
    }

    // Static Methods
    public static MediaType getMediaType(String extension) {
        return switch (extension.toLowerCase()) {
            case ".mkv" -> MKV;
            case ".mp4" -> MP4;
            default -> throw new IllegalArgumentException("Unsupported media type: " + extension);
        };
    }
}