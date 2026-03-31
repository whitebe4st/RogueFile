using System;
using System.Collections.Generic;

[Serializable]
public class DifyInputs
{
    public string npc_name;
    public string npc_id;
    public string game_state;
    public int current_stress;
}

[Serializable]
public class DifyAnswerData
{
    public string reply;
    public int stress_delta;
    public bool is_lying; // placeholder
}

[Serializable]
public class DifyRequest
{
    public string query;
    public string response_mode = "blocking";
    public string user;
    public string conversation_id = "";
    public DifyInputs inputs = new DifyInputs();
}

[Serializable]
public class DifyResponse
{
    public string @event;
    public string message_id;
    public string conversation_id;
    public string answer;
    public long created_at;
}
