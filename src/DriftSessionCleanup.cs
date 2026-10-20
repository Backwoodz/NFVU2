using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
namespace Rachel350Z {
internal sealed class DriftSessionCleanup {
    static readonly FieldInfo Items=typeof(ItemDrop).GetField("s_instances",BindingFlags.Static|BindingFlags.NonPublic);
    int characterCursor,itemCursor;
    // Fixed per-pass work budget; no scene search or whole-world ZDO enumeration.
    public void Tick() {
        if(!DriftSessionRules.Active||ZNetScene.instance==null)return;
        var characters=Character.GetAllCharacters();
        for(int work=0;work<16&&characters.Count>0;work++) {
            characterCursor%=characters.Count;var character=characters[characterCursor++];
            if(character==null)continue;
            var view=character.GetComponent<ZNetView>();
            if(view==null||!view.IsValid()||!view.IsOwner())continue;
            if(DriftSessionRules.RemoveCharacter(character.IsPlayer(),character.IsTamed(),view.GetZDO().GetBool(DriftSessionRules.SpectatorTag)))
                ZNetScene.instance.Destroy(character.gameObject); // No death callback and no creature loot.
        }
        var items=Items?.GetValue(null) as List<ItemDrop>;
        if(items==null)return;
        for(int work=0;work<16&&items.Count>0;work++) {
            itemCursor%=items.Count;var item=items[itemCursor++];
            if(item==null)continue;
            var view=item.GetComponent<ZNetView>();
            if(view==null)continue;
            bool valid=view.IsValid();
            if(DriftSessionRules.RemoveLooseItem(valid,valid&&view.IsOwner(),valid&&view.GetZDO().GetBool(ZDOVars.s_piece)))
                ZNetScene.instance.Destroy(item.gameObject);
        }
    }
}
}
