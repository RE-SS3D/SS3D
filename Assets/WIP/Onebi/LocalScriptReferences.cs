using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class LocalData
{
    private static AuthData authData;
    

    static LocalData()
    {
        authData = new AuthData();
    }

    public static AuthData GetAuthData()
    {
        return authData;
    }
}
