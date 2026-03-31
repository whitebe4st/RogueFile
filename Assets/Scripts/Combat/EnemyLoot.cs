using UnityEngine;
using System.Collections.Generic;

public class EnemyLoot : MonoBehaviour
{
    [System.Serializable]
    public class LootDropData
    {
        public ItemData item;
        [Range(0f, 100f)]
        public float dropChance = 50f; // โอกาสดรอป 0-100%
    }

    [Header("Reward Settings")]
    public int goldDrop = 50;
    public List<LootDropData> possibleLoot = new List<LootDropData>();

    public void DropLoot()
    {
        if (RewardUIManager.instance != null)
        {
            List<ItemData> droppedItems = new List<ItemData>();
            
            foreach (var loot in possibleLoot)
            {
                // สุ่มตัวเลข 0 ถึง 100 แล้วเทียบเปอร์เซ็น
                if (loot.item != null && Random.Range(0f, 100f) <= loot.dropChance)
                {
                    droppedItems.Add(loot.item);
                }
            }

            RewardUIManager.instance.AccumulateReward(goldDrop, droppedItems);
            
            // ตรวจสอบว่ามีศัตรูเหลืออยู่ในฉากกี่ตัว
            // เนื่องจากตัวมันเองกำลังตายและจะถูก Destroy ในเฟรมถัดไป มันจึงยังนับตัวเองรวมอยู่ด้วย (ดังนั้นถ้าเหลือ <= 1 ก็คือหมดฉาก)
            EnemyLoot[] remainingEnemies = FindObjectsOfType<EnemyLoot>();
            if (remainingEnemies.Length <= 1)
            {
                RewardUIManager.instance.ShowAccumulatedReward();
            }
        }
    }
}