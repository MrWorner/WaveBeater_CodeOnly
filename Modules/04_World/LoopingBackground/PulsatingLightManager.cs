using System.Collections.Generic;
using UnityEngine;

public static class PulsatingLightManager
{
    private static List<PulsatingLight> _allPulsatingLights = new List<PulsatingLight>();

    /// <summary>
    /// Регистрирует экземпляр PulsatingLight.
    /// </summary>
    public static void RegisterLight(PulsatingLight light)
    {
        //if (!_allPulsatingLights.Contains(light))
        //{
            _allPulsatingLights.Add(light);
       //}
    }

    /// <summary>
    /// Удаляет экземпляр PulsatingLight из списка.
    /// </summary>
    public static void UnregisterLight(PulsatingLight light)
    {
        if (_allPulsatingLights.Contains(light))
        {
            _allPulsatingLights.Remove(light);
        }
    }

    /// <summary>
    /// Включает пульсацию для всех зарегистрированных объектов.
    /// </summary>
    public static void EnableAllPulsation()
    {
        foreach (var light in _allPulsatingLights)
        {
            light.EnablePulsation();
        }
    }

    /// <summary>
    /// Отключает пульсацию для всех зарегистрированных объектов.
    /// </summary>
    public static void DisableAllPulsation()
    {
        foreach (var light in _allPulsatingLights)
        {
            light.DisablePulsation();
        }
    }
}