using Mirror;
using UnityEngine;

public class AdminUi : NetworkBehaviour
{
    public bool showGui = true;
    public int width = 215;
    public int height = 400;
    public int posX = -1;
    public int posY = 40;

    private RoundManager _roundManager;
    private Rect _windowRect;

    void Awake()
    {
        if (posX == -1)
        {
            posX = Screen.width - width - 10;     
        }
        
        _windowRect = new Rect(posX, posY, width, height);
        
        _roundManager = GameObject.Find("GameManager").GetComponent<RoundManager>();
    }

    void OnGUI()
    {
        if (!showGui)
        {
            return;
        }

        _windowRect = GUILayout.Window(0, _windowRect, DrawAdminUi, "Admin Controls");
    }

    private void DrawAdminUi(int windowId)
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Set Next Round"))
        {
            CmdSetNextRound();
        }
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Start Round"))
        {
            CmdStartNextRound();
        }
        GUILayout.EndHorizontal();
        
        GUILayout.BeginVertical();

        var round = _roundManager.GetCurrentRound();

        var roundName = round.GetDisplayName(true);
        var roundTime = round.GetElapsedTimeDisplay();
        
        GUILayout.Label("Current Round: " + roundName);
        GUILayout.Label("Elapsed Time: " + roundTime);
        
        GUILayout.EndVertical();
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("End Round"))
        {
            CmdEndRound();
        }
        GUILayout.EndHorizontal();

        GUI.DragWindow(new Rect(0, 0, 10000, 20));
    }

    [Command]
    private void CmdSetNextRound()
    {
        _roundManager.SetNextRound(RoundManager.RoundType.Secret);
    }

    [Command]
    private void CmdStartNextRound()
    {
        _roundManager.StartNextRound();
    }
    
    [Command]
    private void CmdEndRound()
    {
        _roundManager.EndRound();
    }
}
