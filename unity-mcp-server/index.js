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
      {
        name: "get_mcp_debug_info",
        description: "Gets debugging and diagnostic information about the LPC Equipment Manager, active slots, equipped items, and child components in the scene.",
        inputSchema: {
          type: "object",
          properties: {},
        },
      },
      {
        name: "equip_lpc_item",
        description: "Equips a specific LPC item to a target slot on the LPCEquipmentManager in the current scene.",
        inputSchema: {
          type: "object",
          properties: {
            item: {
              type: "string",
              description: "The name of the item (e.g. 'PlateArmor').",
            },
            slot: {
              type: "string",
              description: "The target equipment slot name (e.g. 'Torso').",
            },
          },
          required: ["item", "slot"],
        },
      },
      {
        name: "slice_texture",
        description: "Slices a sprite texture or all textures in a directory into grid cells.",
        inputSchema: {
          type: "object",
          properties: {
            path: {
              type: "string",
              description: "The Unity asset path to the texture file or directory (e.g. 'Assets/Arsetmap/MyPack/Sprites').",
            },
            width: {
              type: "integer",
              description: "Width of each cell in pixels (default 64).",
            },
            height: {
              type: "integer",
              description: "Height of each cell in pixels (default 64).",
            },
            ppu: {
              type: "integer",
              description: "Pixels Per Unit (default 64).",
            },
            filter: {
              type: "string",
              description: "Filter mode: Point, Bilinear, Trilinear (default Point).",
            },
            px: {
              type: "number",
              description: "Pivot X from 0.0 to 1.0 (default 0.5).",
            },
            py: {
              type: "number",
              description: "Pivot Y from 0.0 to 1.0 (default 0.05).",
            },
          },
          required: ["path"],
        },
      },
      {
        name: "spawn_prefab",
        description: "Spawns a prefab at the specified X and Y coordinates in the active scene.",
        inputSchema: {
          type: "object",
          properties: {
            prefab: {
              type: "string",
              description: "The Unity asset path to the prefab (e.g. 'Assets/Prefabs/Player.prefab').",
            },
            x: {
              type: "number",
              description: "X coordinate (default 0).",
            },
            y: {
              type: "number",
              description: "Y coordinate (default 0).",
            },
            name: {
              type: "string",
              description: "Custom name for the spawned GameObject.",
            },
          },
          required: ["prefab"],
        },
      },
      {
        name: "invoke_static_method",
        description: "Invokes a static C# method on a specified class in Unity via reflection.",
        inputSchema: {
          type: "object",
          properties: {
            class: {
              type: "string",
              description: "Full name of the class (e.g. 'LPCAnimationImporter').",
            },
            method: {
              type: "string",
              description: "The name of the static method to invoke.",
            },
            args: {
              type: "string",
              description: "Comma-separated list of arguments (e.g. 'Assets/Artset,16').",
            },
          },
          required: ["class", "method"],
        },
      },
      {
        name: "find_gameobjects",
        description: "Searches for GameObjects in the active scene matching a query name or attached component name.",
        inputSchema: {
          type: "object",
          properties: {
            query: {
              type: "string",
              description: "The search query string (e.g. 'Player' or 'LPCSpriteSync').",
            },
          },
          required: ["query"],
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

      case "get_mcp_debug_info": {
        const response = await fetch(`${UNITY_BRIDGE_URL}/debug`);
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

      case "equip_lpc_item": {
        const { item, slot } = args;
        const response = await fetch(`${UNITY_BRIDGE_URL}/equip?item=${encodeURIComponent(item)}&slot=${encodeURIComponent(slot)}`);
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

      case "slice_texture": {
        const { path, width, height, ppu, filter, px, py } = args;
        let url = `${UNITY_BRIDGE_URL}/slice?path=${encodeURIComponent(path)}`;
        if (width !== undefined) url += `&width=${width}`;
        if (height !== undefined) url += `&height=${height}`;
        if (ppu !== undefined) url += `&ppu=${ppu}`;
        if (filter !== undefined) url += `&filter=${encodeURIComponent(filter)}`;
        if (px !== undefined) url += `&px=${px}`;
        if (py !== undefined) url += `&py=${py}`;

        const response = await fetch(url);
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

      case "spawn_prefab": {
        const { prefab, x, y, name } = args;
        let url = `${UNITY_BRIDGE_URL}/spawn?prefab=${encodeURIComponent(prefab)}`;
        if (x !== undefined) url += `&x=${x}`;
        if (y !== undefined) url += `&y=${y}`;
        if (name !== undefined) url += `&name=${encodeURIComponent(name)}`;

        const response = await fetch(url);
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

      case "invoke_static_method": {
        const { class: className, method, args: methodArgs } = args;
        let url = `${UNITY_BRIDGE_URL}/invoke?class=${encodeURIComponent(className)}&method=${encodeURIComponent(method)}`;
        if (methodArgs !== undefined) url += `&args=${encodeURIComponent(methodArgs)}`;

        const response = await fetch(url);
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

      case "find_gameobjects": {
        const { query } = args;
        const response = await fetch(`${UNITY_BRIDGE_URL}/find?query=${encodeURIComponent(query)}`);
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
