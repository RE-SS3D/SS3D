"""Run with artifacts/unity-mcp/venv/bin/python Testing/ui/lobby-pilot.py.

Uses the same MCP endpoint configured in Codex, including during sessions that
predate registration of the server. No direct Unity socket or private protocol.
"""
import asyncio
import json
import os
from pathlib import Path

from fastmcp import Client

ROOT = Path(__file__).resolve().parents[2]
MCP_URL = os.environ.get("SS3D_MCP_URL", "http://127.0.0.1:8080")


async def main():
    output = ROOT / "artifacts/ui"
    before = set(output.glob("*/status.json"))
    async with Client(MCP_URL + "/mcp", timeout=60) as client:
        resources = await client.read_resource("mcpforunity://instances")
        data = json.loads(resources[0].text)
        instances = data.get("instances", [])
        matches = [i for i in instances if i.get("name") == ROOT.name]
        if len(matches) != 1:
            raise RuntimeError(f"Expected one SS3D editor at {ROOT}; instances: {instances}")
        instance = matches[0]
        await client.call_tool("set_active_instance", {"instance": instance["id"]})
        info = json.loads((await client.read_resource("mcpforunity://project/info"))[0].text)
        if Path(info["data"]["projectRoot"]).resolve() != ROOT:
            raise RuntimeError("Connected editor belongs to a different checkout")
        result = await client.call_tool("execute_menu_item", {"menu_path": "SS3D/Testing/Run Lobby UI Pilot"})
        if result.is_error:
            raise RuntimeError(str(result))
        print(result)
        for _ in range(180):
            await asyncio.sleep(1)
            created = set(output.glob("*/status.json")) - before
            if created:
                path = max(created, key=lambda p: p.stat().st_mtime)
                try:
                    report = json.loads(path.read_text())
                except json.JSONDecodeError:
                    continue
                if report["status"] in ("passed", "failed"):
                    print(path)
                    print(json.dumps(report, indent=2))
                    return 0 if report["status"] == "passed" else 1
        raise TimeoutError("No completed report after 180 seconds; inspect compilation, Play Mode and editor.log.")


if __name__ == "__main__":
    raise SystemExit(asyncio.run(main()))
