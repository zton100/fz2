using EquipmentIdle.Net;
using System.Collections.Generic;

namespace EquipmentIdle.UI
{
    public partial class MainController
    {
        private void ToggleLock(EquipmentDTO eq)
        {
            if (eq == null) return;
            bool willLock = !_lockedEquipment.Contains(eq.uid);
            _gameState.LockEquipment(eq.uid, willLock);
            if (willLock)
            {
                AddToast($"已发送锁定：{eq.name}", ToastDuration);
            }
            else
            {
                AddToast($"已发送解锁：{eq.name}", ToastDuration);
            }
        }

        private void DecomposeFromUI(EquipmentDTO eq)
        {
            if (eq == null) return;
            if (_lockedEquipment.Contains(eq.uid))
            {
                AddToast($"已锁定，不能分解：{eq.name}", ToastDuration);
                return;
            }
            AddToast($"已发送分解：{eq.name}", ToastDuration);
            _gameState.Decompose(eq.uid);
        }

        private void PruneLockedEquipment()
        {
            if (_lockedEquipment.Count == 0) return;
            var existing = new HashSet<string>();
            foreach (var eq in _gameState.Bag)
            {
                if (eq != null) existing.Add(eq.uid);
            }
            foreach (var eq in _gameState.Equipped)
            {
                if (eq != null) existing.Add(eq.uid);
            }
            _lockedEquipment.RemoveWhere(uid => !existing.Contains(uid));
        }

        private void SyncLockedEquipment(List<EquipmentDTO> bag, List<EquipmentDTO> equipped)
        {
            _lockedEquipment.Clear();
            if (bag != null)
            {
                foreach (var eq in bag)
                {
                    if (eq != null && eq.locked) _lockedEquipment.Add(eq.uid);
                }
            }
            if (equipped != null)
            {
                foreach (var eq in equipped)
                {
                    if (eq != null && eq.locked) _lockedEquipment.Add(eq.uid);
                }
            }
        }
    }
}
