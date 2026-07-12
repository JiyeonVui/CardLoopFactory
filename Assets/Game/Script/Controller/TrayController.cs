using System;
using System.Collections.Generic;
using System.Linq;
using Extension;

[Service(nameof(ITrayController))]
public interface ITrayController
{
    void Init(LevelManager level);
    void RemoveTrayModel(TrayModel trayModel);
    void OnCardDistributed(TrayModel trayModel);
    void UnlockTraysBlockedBy(int completedTrayId);
}

public class TrayController : ITrayController
{
    private LevelManager _levelManager;
    
    public void Init(LevelManager level)                 
    {                                                    
        _levelManager = level;                           
    }
    
    public void RemoveTrayModel(TrayModel trayModel )
    {
        _levelManager.TrayModels.Remove(trayModel);
    }

    // Called once per card as it leaves the tray. Counts down and empties the
    // tray when no cards remain.
    public void OnCardDistributed(TrayModel trayModel)
    {
        if (trayModel == null)
        {
            return;
        }

        trayModel.TotalCardCount--;

        if (trayModel.TotalCardCount <= 0)
        {
            trayModel.State = TrayState.Empty;
        }
    }
    public void UnlockTraysBlockedBy(int completedTrayId)
    {                                                    
        if (_levelManager?.TrayModels == null)           
        {                                                
            return;                                      
        }                                                
                                                       
        foreach (TrayModel tray in _levelManager.TrayModels)                            
        {                                                
            if (!tray.IsLocked || tray.BlockedByTrayIds  == null)                                             
            {                                            
                continue;                                
            }                                            
            
            tray.BlockedByTrayIds.Remove(completedTrayId);       
                                                       
            if (tray.BlockedByTrayIds.Count == 0)        
            {                                            
                tray.IsLocked = false;                   
            }                                            
        }                                                
    }   
}   