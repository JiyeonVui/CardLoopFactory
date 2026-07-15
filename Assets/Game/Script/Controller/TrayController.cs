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

    // Còn tray đã rỗng (đã phát hết card) đang nằm trên sân, chưa move lên slot và
    // không bị khoá -> người chơi còn nước tạo slot match-color mới. Dùng để check thua.
    bool HasEmptyTrayToMove();
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
    // Tray rỗng còn trên sân = State Empty (đã phát hết card) nhưng chưa move lên slot
    // (lúc move sẽ set IsUsed) và không bị khoá.
    public bool HasEmptyTrayToMove()
    {
        if (_levelManager?.TrayModels == null)
        {
            return false;
        }

        return _levelManager.TrayModels.Any(tray => tray.State == TrayState.Empty && !tray.IsLocked);
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