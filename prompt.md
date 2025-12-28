 
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

請你幫我把整個資料夾讀取過一遍，這是我的一個遊戲專案使用unity6000是用C#編寫的，尤其是DoorTrigger和DoorController的程式碼，因為這部分有一點問題，我理想上是他會去偵測player的位置去改變開門的方向，但是實際上我去更改open angle postive和open angle negative的值，他只會看open angle postive的值，去改變方向，請你幫我看一下![alt text](image-2.png)

對的，我有把門轉了 90 度

我還有一個問題就是我的key本來是針對一個門一個門的，但是現在我把門放在一起，讓他變成雙開門，所以我希望可以新增一個部分是可以針對多個門的的綁定，用一支鑰匙同時開兩扇門。

這幾個在寶箱上的屬性是什麼![alt text](image-3.png)

檢查Door和Chest的程式碼，因為我原本是要做手持鑰匙才會開啟但現在是只要身上有鑰匙就可以開啟，請你幫我看一下

寶相開啟時有延遲，就是他會等一下才銷毀和掉落東西，這是可以讓它不要有延遲的嗎

zombie中哪一個是調整他的偵測範圍

現在zombie有一個問題就是他會被player擊退到wall裡面，請你幫我看一下

你幫我檢查一下player的程式碼因為我把player的circle collider的size改大了，但他就受不了傷了，請你幫我看一下

現在player被zombie擊退時，有大機率會開始原地旋轉或抖動，請你幫我調整一下，或跟我說要怎麼調整

以上對zombie的調整你有把RangedZombie也算進去一起調整嗎，如果沒有請你調整，然後關於RangedZombie的攻擊方式，我的想法是他在進入一定範圍後改為近戰攻擊，但並沒有實施，請你幫我看一下

我有一個問題就是，我把Pistol放入chest裡面，Pistol掉出來後player撿起來Pistol可以開槍也有聲音但不會有子彈也會有莫名的擊退player的效果

有辦法讓player身處的房間是可見的，就是會受到Global Light 2D所影響，而在房間外的則是全暗嗎，請先不要修改，先跟我討論

我想用「方案二（局部光）」。我的地圖是用 Tilemap 做的，沒有把房間分成了獨立的物件。

好的請幫我寫一個局部光的系統和教我設定。有 0.5 秒左右的漸變、牆壁有設定 Shadow Caster 2D、大廳連接小房間分開亮。

他只有呈現一個星星狀的光，沒有呈現出整個房間的光，請你幫我看一下

我使用global light 2d 是可以的，但他是整個場景都亮起來，這是可以調整的嗎，還是只能用Sprite Light 2D

只有這些選項![alt text](image-4.png)

這些設定分別是什麼![alt text](image-5.png)

我的房間中有其他物件也有shadow caster 2d，是可以讓它不會被局部燈照到而產生陰影嗎，就讓它只會被player的flashlight影響

我還有地板但它沒辦法通時選擇兩個sorting layers，我想要它可以被flashlight照到和局部燈照到，但它們部會產生陰影，只會亮而已

等等我的shadow的設定只有這些![alt text](image-6.png)

![alt text](image-7.png)這是什麼

好了我知道了，回歸到最原始的設定，就是到我提出free light的設定那裡

因為我只是要有局部光而已，所以根本就不需要什麼影子，因此我把這個關了![alt text](image-8.png)

子彈還是依然會射到player並造成傷害，請你幫我看一下

我想解決一個問題就是Pistol它擷取不到Ammo的圖片和字，請你幫我看一下

因為我的圖片和字是object，所以他一直顯示type mismatch，請你幫我看一下

這是我想放的圖片和字，這是結果![alt text](image-9.png)![alt text](image-10.png)

你可以幫我檢查plyer或是UI的程式碼裡面是否有設定子彈圖標的初始值嗎，因為player只要手裡是空的，圖標就會顯示弓箭的樣子，請顯示彈藥的樣子

我想要把AK和pistol的子彈消耗數量分開，但他們的程式碼都是pistol，所以請你幫我修改，並且幫我加入能夠補充彈藥的物件，也要分AK和pistol程式碼可以視同一個，但要有選項可以選擇他是哪一種彈藥，請你幫我看一下

我想請問一個問題是局部燈每一個都是分開的嗎，因為我怕我觸發其中一個的的時候另一個也會觸發，請你幫我看一下

所以我可以把它變成prefabs嗎