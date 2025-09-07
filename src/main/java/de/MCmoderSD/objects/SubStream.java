package de.MCmoderSD.objects;

import com.fasterxml.jackson.databind.JsonNode;
import de.MCmoderSD.enums.Language;

public class SubStream {

    // Attributes
    private final int id;
    private final boolean defaultTrack;
    private final boolean enabledTrack;
    private final boolean forcedTrack;
    private final Language language;

    // Constructor
    public SubStream(JsonNode subStream) {
        var properties = subStream.get("properties");
        id = subStream.get("id").asInt();
        defaultTrack = properties.get("default_track").asBoolean();
        enabledTrack = properties.get("enabled_track").asBoolean();
        forcedTrack = properties.get("forced_track").asBoolean();
        language = Language.getLanguage(properties.get("language").asText());
    }

    // Getters
    public int getId() {
        return id;
    }

    public boolean isDefaultTrack() {
        return defaultTrack;
    }

    public boolean isEnabledTrack() {
        return enabledTrack;
    }

    public boolean isForcedTrack() {
        return forcedTrack;
    }

    public Language getLanguage() {
        return language;
    }
}