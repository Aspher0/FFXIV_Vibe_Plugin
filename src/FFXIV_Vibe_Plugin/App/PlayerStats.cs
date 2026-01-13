using FFXIV_Vibe_Plugin.App;
using System;

namespace FFXIV_Vibe_Plugin;

public class PlayerStats
{
    private float _CurrentHp => Service.ConnectedPlayerObject?.CurrentHp ?? -1f;
    private float _MaxHp => Service.ConnectedPlayerObject?.MaxHp ?? -1f;

    private float _prevCurrentHp = -1f;
    private float _prevMaxHp = -1f;
    public string PlayerName => Service.ConnectedPlayerObject?.Name.TextValue ?? "*unknown*";

    public event EventHandler? Event_CurrentHpChanged;

    public event EventHandler? Event_MaxHpChanged;


    public void Update()
    {
        if (Service.ConnectedPlayerObject == null)
            return;

        UpdateCurrentHp();
    }


    public string GetPlayerName() => PlayerName;

    private void UpdateCurrentHp()
    {
        if (_CurrentHp != _prevCurrentHp)
        {
            EventHandler currentHpChanged = Event_CurrentHpChanged;

            if (currentHpChanged != null)
                currentHpChanged(this, EventArgs.Empty);
        }

        if (_MaxHp != _prevMaxHp)
        {
            EventHandler eventMaxHpChanged = Event_MaxHpChanged;

            if (eventMaxHpChanged != null)
                eventMaxHpChanged(this, EventArgs.Empty);
        }

        _prevCurrentHp = _CurrentHp;
        _prevMaxHp = _MaxHp;
    }

    public float GetCurrentHP() => _CurrentHp;

    public float GetMaxHP() => _MaxHp;
}
