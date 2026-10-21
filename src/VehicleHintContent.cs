using System;
using System.Collections.Generic;

namespace Rachel350Z {
// Physical controls, deliberately independent of Valheim's remappable combat
// bindings. Sprite names match ZInput's xbox/ps5 sprite map.
public static class VehicleHintContent {
    public static string Glyph(string xbox,string ps,bool playstation)=>"<sprite=\""+(playstation?"ps5":"xbox")+"\" name=\""+(playstation?ps:xbox)+"\">";
    public static string[] Build(bool controller,bool ps,bool flight,bool menu,bool hummer,string menuKey,Func<string,string> tr,bool driftWorld=false) {
        var rows=new List<string>();
        Func<string,string,string> icon=(x,p)=>Glyph(x,p,ps);
        Action<string,string> row=(key,label)=>rows.Add(key+"  "+tr(label));
        string close=controller?icon("button_ls","button_l3"):menuKey;
        if(menu) {
            row(close,"Menü schließen");
            if(controller)row(icon("button_b","button_circle"),"Zurück");
            row(controller?icon("button_a","button_cross"):"LMB",controller?"Wählen / bearbeiten":"Auswählen");
            row(controller?icon("lstick","lstick"):"↑ / ↓",controller?"Navigation":"Kategorie");
            if(controller)row(icon("button_lb","button_l1")+" / "+icon("button_rb","button_r1"),"Kategorien / Inhalt");
            row(controller?icon("rstick","rstick"):"RMB","Kamera");
            return rows.ToArray();
        }
        row(controller?icon("button_rt","button_r2"):"W",flight?"Vorwärts":"Gas");
        row(controller?icon("button_lt","button_l2"):"S",flight?"Rückwärts":"Bremsen / rückwärts");
        row(controller?icon("lstick","lstick"):"A / D","Lenken");
        row(controller?icon("rstick","rstick"):tr("Maus"),"Kamera");
        if(flight) {
            row(controller?icon("button_rb","button_r1"):"Shift","Steigen");
            row(controller?icon("button_lb","button_l1"):tr("Leertaste"),"Sinken");
        }else {
            row(controller?icon("button_a","button_cross"):tr("Leertaste"),"Handbremse");
            row(controller?icon("button_rt","button_r2")+" + "+icon("button_lt","button_l2"):"W + S","Burnout im Stand");
            row(controller?icon("button_lb","button_l1"):"RMB","Geschütz feuern");
        }
        if(!hummer||flight)row(controller?icon("button_x","button_square"):"LMB / "+tr("Strg"),"Nitro");
        if(!driftWorld)row(controller?icon("button_b","button_circle"):"E",flight?"Landen & aussteigen":"Aussteigen");
        row(controller?icon("dpad_left","dpad_left")+" / "+icon("dpad_right","dpad_right"):"← / →","Songs");
        if(!driftWorld)row(controller?icon("dpad_up","dpad_up")+" / "+icon("dpad_down","dpad_down"):"1–4","Fahrmodus");
        row(close,"Modmenü");
        row(controller?icon("button_rs","button_r3"):"H","Hupe");
        if(flight)row("", "Geschütz: automatisch");
        return rows.ToArray();
    }
}
}
