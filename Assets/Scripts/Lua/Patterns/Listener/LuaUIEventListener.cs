using System.Collections.Generic;
using UnityEngine.EventSystems;

public class LuaUIEventListener : LuaBaseListener
{
    private EventTrigger eventTrigger;
    private Dictionary<EventTriggerType, EventTrigger.Entry> entryMap = new Dictionary<EventTriggerType, EventTrigger.Entry>();

    private void Awake()
    {
        eventTrigger = GetComponent<EventTrigger>() ?? gameObject.AddComponent<EventTrigger>();
    }

    public void AddEntry(EventTriggerType eventID)
    {
        if (entryMap.ContainsKey(eventID))
        {
            entryMap[eventID].callback.AddListener(evtData =>
            {
                Call(eventID, evtData);
            });
        }
        else
        {
            var entry = new EventTrigger.Entry();
            entry.eventID = eventID;
            entry.callback.AddListener(evtData =>
            {
                Call(eventID, evtData);
            });
            eventTrigger.triggers.Add(entry);
            entryMap.Add(eventID, entry);
        }
    }
}
