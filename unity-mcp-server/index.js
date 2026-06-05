import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
} from "@modelcontextprotocol/sdk/types.js";

import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

function getUnityBridgePort() {
  const defaultPort = 8035;
  try {
    // Thử đọc từ Temp/unity-mcp-port.txt ở thư mục cha (khi chạy từ thư mục con unity-mcp-server)
    const path1 = path.join(__dirname, "..", "Temp", "unity-mcp-port.txt");
    if (fs.existsSync(path1)) {
      const portStr = fs.readFileSync(path1, "utf8").trim();
      const port = parseInt(portStr, 10);
      if (!isNaN(port)) return port;
    }
    
    // Thử đọc từ Temp/unity-mcp-port.txt ở CWD (khi chạy ở project root)
    const path2 = path.join(process.cwd(), "Temp", "unity-mcp-port.txt");
    if (fs.existsSync(path2)) {
      const portStr = fs.readFileSync(path2, "utf8").trim();
      const port = parseInt(portStr, 10);
      if (!isNaN(port)) return port;
    }
  } catch (e) {
    console.error("Error reading unity-mcp-port.txt:", e.message);
  }
  return defaultPort;
}

const UNITY_BRIDGE_PORT = getUnityBridgePort();
const UNITY_BRIDGE_URL = `http://127.0.0.1:${UNITY_BRIDGE_PORT}`;
console.error(`[Unity MCP Config] Connecting to Bridge: ${UNITY_BRIDGE_URL}`);

// Initialize MCP Server
const server = new Server(
  {
    name: "unity-mcp-server",
    version: "1.1.0",
  },
  {
    capabilities: {
      tools: {},
    },
  }
);

// Register Tools
server.setRequestHandler(ListToolsRequestSchema, async () => {
  return {
    tools: [
      {
        name: "get_scene_hierarchy",
        description: "Retrieves the full GameObject hierarchy of the currently active Unity scene.",
        inputSchema: {
          type: "object",
          properties: {},
        },
      },
      {
        name: "inspect_gameobject",
        description: "Inspects a specific GameObject by its unique Instance ID, returning its position, rotation, scale, and details of all attached components (both public fields and private [SerializeField] fields).",
        inputSchema: {
          type: "object",
          properties: {
            id: {
              type: "integer",
              description: "The unique Instance ID of the GameObject to inspect.",
            },
          },
          required: ["id"],
        },
      },
      {
        name: "update_component_value",
        description: "Modifies the value of a specific field (variable) in a component attached to a GameObject. Automatically parses strings into int, float, bool, or enums, and marks the scene dirty to save changes.",
        inputSchema: {
          type: "object",
          properties: {
            id: {
              type: "integer",
              description: "The unique Instance ID of the GameObject.",
            },
            component: {
              type: "string",
              description: "The name of the target Component (e.g. 'LPCPlayerController2').",
            },
            field: {
              type: "string",
              description: "The name of the variable field to update (e.g. 'moveSpeed').",
            },
            value: {
              type: "string",
              description: "The new value to set, formatted as a string.",
            },
          },
          required: ["id", "component", "field", "value"],
        },
      },
      {
        name: "get_console_logs",
        description: "Fetches the latest 100 log messages (info, warnings, errors) printed in the Unity Editor Console.",
        inputSchema: {
          type: "object",
          properties: {},
        },
      },
      {
        name: "set_play_mode",
        description: "Controls the Play Mode state of the Unity Editor (Start, Pause, or Stop the game).",
        inputSchema: {
          type: "object",
          properties: {
            state: {
              type: "string",
              enum: ["play", "pause", "stop"],
              description: "The target Play Mode state.",
            },
          },
          required: ["state"],
        },
      },
    ],
  };
});

// Handle Tool Execution
server.setRequestHandler(CallToolRequestSchema, async (request) => {
  const { name, arguments: args } = request.params;

  try {
    switch (name) {
      case "get_scene_hierarchy": {
        const response = await fetch(`${UNITY_BRIDGE_URL}/hierarchy`);
        if (!response.ok) {
          throw new Error(`Unity Bridge returned status ${response.status}`);
        }
        const data = await response.json();
        return {
          content: [
            {
              type: "text",
              text: JSON.stringify(data, null, 2),
            },
          ],
        };
      }

      case "inspect_gameobject": {
        const { id } = args;
        const response = await fetch(`${UNITY_BRIDGE_URL}/inspect?id=${id}`);
        if (!response.ok) {
          if (response.status === 404) {
            return {
              content: [
                {
                  type: "text",
                  text: `GameObject with ID ${id} not found in the current scene.`,
                },
              ],
              isError: true,
            };
          }
          throw new Error(`Unity Bridge returned status ${response.status}`);
        }
        const data = await response.json();
        return {
          content: [
            {
              type: "text",
              text: JSON.stringify(data, null, 2),
            },
          ],
        };
      }

      case "update_component_value": {
        const { id, component, field, value } = args;
        const url = `${UNITY_BRIDGE_URL}/update?id=${id}&component=${encodeURIComponent(component)}&field=${encodeURIComponent(field)}&value=${encodeURIComponent(value)}`;
        const response = await fetch(url);
        
        if (!response.ok) {
          const errorData = await response.json().catch(() => ({ error: `Status ${response.status}` }));
          return {
            content: [
              {
                type: "text",
                text: `Failed to update component: ${errorData.error}`,
              },
            ],
            isError: true,
          };
        }
        
        const data = await response.json();
        return {
          content: [
            {
              type: "text",
              text: `Success: Field '${field}' on component '${component}' updated successfully. Bridge Response: ${JSON.stringify(data, null, 2)}`,
            },
          ],
        };
      }

      case "get_console_logs": {
        const response = await fetch(`${UNITY_BRIDGE_URL}/logs`);
        if (!response.ok) {
          throw new Error(`Unity Bridge returned status ${response.status}`);
        }
        const data = await response.json();
        return {
          content: [
            {
              type: "text",
              text: JSON.stringify(data, null, 2),
            },
          ],
        };
      }

      case "set_play_mode": {
        const { state } = args;
        const response = await fetch(`${UNITY_BRIDGE_URL}/playmode?state=${state}`);
        if (!response.ok) {
          throw new Error(`Unity Bridge returned status ${response.status}`);
        }
        const data = await response.json();
        return {
          content: [
            {
              type: "text",
              text: `Unity Play Mode set to '${state}' successfully. Bridge Response: ${JSON.stringify(data)}`,
            },
          ],
        };
      }

      default:
        throw new Error(`Unknown tool: ${name}`);
    }
  } catch (error) {
    return {
      content: [
        {
          type: "text",
          text: `Error communicating with Unity Editor. Make sure Unity is running and the UnityMCPBridge script is initialized! Error details: ${error.message}`,
        },
      ],
      isError: true,
    };
  }
});

// Run Server
async function main() {
  const transport = new StdioServerTransport();
  await server.connect(transport);
  console.error("Unity MCP Server running via stdio");
}

main().catch((error) => {
  console.error("Fatal error in main:", error);
  process.exit(1);
});
