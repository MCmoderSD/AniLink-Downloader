package de.MCmoderSD.utilities;

import de.MCmoderSD.json.JsonUtility;
import tools.jackson.databind.JsonNode;

public class ConfigProcessor {
    
    // Attributes
    private Long delay;
    private String password;
    private String apiKey;
    
    // Constructor
    public ConfigProcessor() {
        
        // Nullify
        delay = null;
        password = null;
        apiKey = null;
        
        // Try to load config
        var config = loadConfig();
        parseConfig(config);

        // Prompt delay
        if (delay == null) {
            var input = IO.readln("Enter delay between downloads in milliseconds (default: 500):\n").trim();
            try {
                delay = Long.parseLong(input);
            } catch (NumberFormatException e) {
                System.err.println("Invalid delay format. Using default value 500ms.\n");
                delay = 500L;
            }
        }

        // Prompt Password
        if (password == null) password = IO.readln("Enter password for protected links (leave empty if not needed): \n").trim();

        // Prompt API Key
        if (apiKey == null) apiKey = IO.readln("Enter Debrid-Link API Key: \n").trim();
    }
    
    private JsonNode loadConfig() {
        try {
            return JsonUtility.getInstance().loadResource("/config.json");
        } catch (Exception e) {
            return null;
        }
    }
    
    private void parseConfig(JsonNode config) {
        if (config == null || config.isNull() || config.isEmpty()) return;
        if (config.has("delay") && config.get("delay").isNumber()) delay = config.get("delay").asLong();
        if (config.has("password") && config.get("password").isString()) password = config.get("password").asString();
        if (config.has("apiKey") && config.get("apiKey").isString()) apiKey = config.get("apiKey").asString();
    }

    public long getDelay() {
        return delay;
    }

    public String getPassword() {
        return password;
    }

    public String getApiKey() {
        return apiKey;
    }
}