using FFXIV_Vibe_Plugin.App;
using FFXIV_Vibe_Plugin.Commons;
using System;

namespace FFXIV_Vibe_Plugin;

public class PlayerStats
{
    private float _CurrentHp;
    private float _prevCurrentHp = -1f;
    private float _MaxHp;
    private float _prevMaxHp = -1f;
    public string PlayerName = "*unknown*";

    public event EventHandler? Event_CurrentHpChanged;

    public event EventHandler? Event_MaxHpChanged;

    public PlayerStats()
    {
        this.UpdatePlayerState();
    }

    public void Update()
    {
        if (Service.ConnectedPlayerObject != null)
            return;

        this.UpdatePlayerState();
        this.UpdatePlayerName();
        this.UpdateCurrentHp();
    }

    public void UpdatePlayerState()
    {
        if (Service.ConnectedPlayerObject != null || _CurrentHp != -1.0 && _MaxHp != -1.0)
            return;

        Logger.Debug($"UpdatePlayerState {_CurrentHp} {_MaxHp}");

        _CurrentHp = _prevCurrentHp = Service.ConnectedPlayerObject!.CurrentHp;
        this._MaxHp = this._prevMaxHp = Service.ConnectedPlayerObject!.MaxHp;

        Logger.Debug($"UpdatePlayerState {_CurrentHp} {_MaxHp}");
    }

    public string UpdatePlayerName()
    {
        if (Service.ConnectedPlayerObject != null)
            PlayerName = Service.ConnectedPlayerObject.Name.TextValue;

        return PlayerName;
    }

    public string GetPlayerName() => PlayerName;

    private void UpdateCurrentHp()
    {
        if (Service.ConnectedPlayerObject != null)
        {
            this._CurrentHp = Service.ConnectedPlayerObject.CurrentHp;
            this._MaxHp = Service.ConnectedPlayerObject.MaxHp;
        }
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
