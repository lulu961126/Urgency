 
請你幫我把整個資料夾讀取過一遍，這是我的一個遊戲專案使用unity6000是用C#編寫的， 

請幫我釐清以下的問題:
1.我有一個Boss他的程式在Assets\Scripts\Mobs\RangedZombie.cs請你先幫我看一下
2.我的Player的操控在Assets\Scripts\Player\Player.cs
3.我的近戰武器操控的程式碼在Assets\Scripts\Weapons\MeleeWeapon.cs

我希望你幫我釐清player用近戰攻擊boss時不會產生任何的傷害，請幫我釐清問題在哪裡，如果你有什麼問題請你先跟我說

我有一個問題想請教你，你有找到在Assets\Scripts\Mobs\RangedZombie.cs和Assets\Scripts\Mobs\Zombie.cs中有使用NavMesh的PackManager的套件嗎

是的，但這裡你好像無法看到有關NavMeshSurface的東西![alt text](image.png)

在我兩個zombie的程式碼中，我應改有加入類似的偵測障礙物的程式碼了，請你幫我看一下

Zombie 會卡在某些障礙物上和迴避效果不理想如果可以幫我用NavMesh的話會更好，因為我有看過覺得NavMesh比較好

還是有辦法不用NavMesh達到更好的效果，就是單純的程式碼控制，請你幫我看一下

我想要改變zombie們的扣血邏輯，player.cs中應該有血量和護甲值得設定，請你幫我改成受到傷害優先扣護甲值，護甲扣完才扣生命值，請你幫我看一下

你能找到chestDrop 2D、KeyPickup2D、Keyring、RequiresKeyAnyOf2D、DoorController、DoorTrigger的程式碼嗎，我想要讓鑰匙可以打開門或寶箱，但我之前做出太雜亂的東西請你幫我優化和整理，我的想法是放在key上的程式碼可以幫我分類選擇此物件是DoorKey還是ChestKey讓他們的功能不一樣。還有們的部分我想讓他去偵測player的位置讓他可以更合理的開門，就是他會朝玩家面向的方向打開門。Door和Chest也請幫我做出能夠選擇要不要鎖起來，這是我門的配置方式。![alt text](image-1.png)

我的遊戲是2d的並且出現這個問題Assets\Scripts\Object\DoorController.cs(322,27): error CS0034: Operator '+' is ambiguous on operands of type 'Vector2' and 'Vector3'

我還有一點問題，就是我其實已經有拾取地板物件ItemWorldPickup、丟棄物件EquippedItemDisplay、物品欄ContainerManager這些了，所以Keyring其實可以不用了，請幫我把開鎖就是開門和寶箱的開啟方式改成拿鑰匙靠近就可以了，不用按E什麼的，門的部分就是解鎖後就把他的狀態改成打開的，寶箱部分就還好因為開啟後他就會消失了。

真的有辦法做滾輪選物品嗎

請你幫我找到Pistol.cs的程式碼，他在做射擊時消耗的彈藥圖案在螢幕中顯示是錯的，請你幫我看一下，會讓我可以選擇我要的是哪一個。我應該有在裡面加入空彈藥時會產生音效，你幫我檢查一下

你幫我把整個專案都看過一次並且幫我把有問題的都先列出來，能夠優化的也都幫我列出來，先不要做任何的修改，只要列出來等待我討論。

如果都請你幫忙修復和優化是可以的嗎

一個階段一個階段用好了，先用第一階段

Assets\Scripts\Weapons\Bow.cs(178,13): error CS0103: The name 'arrowPattern' does not exist in the current context
Assets\Scripts\Weapons\Bow.cs(178,27): error CS0103: The name 'arrowPattern' does not exist in the current context

好的，修改第二階段

好的，進行第三階段


