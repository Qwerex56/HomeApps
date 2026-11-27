namespace MacDeviceEmulator.Protocol;

public static class Descriptors
{
    public const string SatelliteDescriptor = """
    {
      "format": "homeapps.device/1",
      "model": "apple/mac-satellite-test",
      "version": "1.0.0",
      "purpose": "assistant",
      "roles": ["satellite"],
      "capabilities": [
        { "type": "voice" },
        { "type": "audio" }
      ],
      "jarvis": "Mac satellite test device with speech synthesis and volume.",
      "heartbeat": 60,
      "controls": [
        {
          "name": "volume",
          "label": "Volume",
          "value": { "type": "integer", "minimum": 0, "maximum": 100 },
          "widget": "slider"
        },
        {
          "name": "muted",
          "label": "Muted",
          "value": { "type": "boolean" },
          "widget": "switch"
        }
      ],
      "actions": [
        {
          "name": "say",
          "label": "Speak text",
          "description": "Speaks text aloud on Mac using say command",
          "input": {
            "type": "object",
            "properties": {
              "text": { "type": "string", "maxLength": 200 }
            },
            "required": ["text"]
          }
        }
      ]
    }
    """;

    public const string PoweredDescriptor = """
    {
      "format": "homeapps.device/1",
      "model": "apple/mac-powered-test",
      "version": "1.0.0",
      "purpose": "smart_home",
      "roles": ["light"],
      "capabilities": [
        { "type": "audio" },
        { "type": "switch", "id": "lamp" }
      ],
      "jarvis": "Mac smart home powered test device with volume, mute, CPU sensor and simulated desk lamp.",
      "heartbeat": 60,
      "controls": [
        {
          "name": "volume",
          "label": "Mac Volume",
          "value": { "type": "integer", "minimum": 0, "maximum": 100 },
          "widget": "slider"
        },
        {
          "name": "muted",
          "label": "Mac Mute",
          "value": { "type": "boolean" },
          "widget": "switch"
        },
        {
          "name": "lamp.on",
          "label": "Desk Lamp",
          "value": { "type": "boolean" },
          "widget": "switch"
        }
      ],
      "sensors": [
        {
          "name": "cpu_load",
          "label": "CPU Load",
          "value": { "type": "number", "minimum": 0, "maximum": 100 },
          "unit": "%"
        }
      ]
    }
    """;

    public const string BatteryDescriptor = """
    {
      "format": "homeapps.device/1",
      "model": "apple/mac-battery-test",
      "version": "1.0.0",
      "purpose": "smart_home",
      "roles": ["weather_station"],
      "sleepy": true,
      "heartbeat": 60,
      "capabilities": [
        { "type": "power" }
      ],
      "jarvis": "Mac battery smart home test device reporting real battery via pmset every 60s.",
      "controls": [
        {
          "name": "level",
          "label": "Level",
          "value": { "type": "integer", "minimum": 0, "maximum": 3 }
        }
      ],
      "sensors": [
        {
          "name": "battery",
          "label": "Battery Level",
          "value": { "type": "number", "minimum": 0, "maximum": 100 },
          "unit": "%"
        }
      ]
    }
    """;

    public const string ComputerDescriptor = """
    {
      "format": "homeapps.device/1",
      "model": "apple/mac-computer-test",
      "version": "1.0.0",
      "purpose": "computer",
      "roles": [],
      "capabilities": [
        { "type": "audio" }
      ],
      "jarvis": "Mac computer test device connected on link with user bearer token.",
      "controls": [
        {
          "name": "volume",
          "label": "Volume",
          "value": { "type": "integer", "minimum": 0, "maximum": 100 },
          "widget": "slider"
        },
        {
          "name": "muted",
          "label": "Mute",
          "value": { "type": "boolean" },
          "widget": "switch"
        }
      ],
      "actions": [
        {
          "name": "say",
          "label": "Speak text",
          "description": "Speaks text aloud on Mac",
          "input": {
            "type": "object",
            "properties": {
              "text": { "type": "string", "maxLength": 200 }
            },
            "required": ["text"]
          }
        }
      ]
    }
    """;
}
