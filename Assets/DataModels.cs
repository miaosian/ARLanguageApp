using System;
using System.Collections.Generic;

[Serializable]
public class ScenarioData
{
    public string title;
    public string description;
    public string personaPrompt;
    public string backgroundUrl;
    public string id;              // e.g. "scenario_007" or any unique key
    public string contextName;     // e.g. "OrderingFood", "Baking", etc.
}


[Serializable]
public class ContextType {
  public string key;
  public string name;
  public string imageUrl;
  public Dictionary<string,ScenarioData> scenarios;
}


[Serializable]
public class ChatMessage
{
    public string role;    // "system", "user", "assistant"
    public string content;
}
