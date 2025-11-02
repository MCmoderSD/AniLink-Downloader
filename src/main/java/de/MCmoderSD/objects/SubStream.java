package de.MCmoderSD.objects;

import de.MCmoderSD.enums.Language;

import tools.jackson.databind.JsonNode;

public class SubStream {

    // Attributes
    private final int id;
    private final boolean defaultTrack;
    private final boolean enabledTrack;
    private final boolean forcedTrack;
    private final Language language;

    // Constructor
    public SubStream(JsonNode subStream) {

        // Check subStream
        if (subStream == null || subStream.isNull() || subStream.isEmpty()) throw new IllegalArgumentException("SubStream JSON node is null or empty");
        if (!subStream.has("id") || subStream.get("id").isNull() || !subStream.get("id").isNumber()) throw new IllegalArgumentException("SubStream JSON node is missing 'id' or it is not a number");
        if (!subStream.has("properties") || subStream.get("properties").isNull() || !subStream.get("properties").isObject()) throw new IllegalArgumentException("SubStream JSON node is missing 'properties' or it is not an object");

        // Parse subStream
        id = subStream.get("id").asInt();
        JsonNode properties = subStream.get("properties");

        // Check properties
        if (!properties.has("default_track") || properties.get("default_track").isNull() || !properties.get("default_track").isBoolean()) throw new IllegalArgumentException("SubStream properties is missing 'default_track' or it is not a boolean");
        if (!properties.has("enabled_track") || properties.get("enabled_track").isNull() || !properties.get("enabled_track").isBoolean()) throw new IllegalArgumentException("SubStream properties is missing 'enabled_track' or it is not a boolean");
        if (!properties.has("forced_track") || properties.get("forced_track").isNull() || !properties.get("forced_track").isBoolean()) throw new IllegalArgumentException("SubStream properties is missing 'forced_track' or it is not a boolean");
        if (!properties.has("language") || properties.get("language").isNull() || !properties.get("language").isString()) throw new IllegalArgumentException("SubStream properties is missing 'language' or it is not a string");

        // Parse properties
        defaultTrack = properties.get("default_track").asBoolean();
        enabledTrack = properties.get("enabled_track").asBoolean();
        forcedTrack = properties.get("forced_track").asBoolean();
        language = Language.getLanguage(properties.get("language").asString());
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