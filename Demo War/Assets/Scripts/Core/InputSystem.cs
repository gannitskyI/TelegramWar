using System.Collections;
using UnityEngine;

public class InputSystem : IInitializable
{
    public int InitializationOrder => 0;

    private InputReader inputReader;

    public IEnumerator Initialize()
    {
        // ������� �������� ��������� �� Resources
        inputReader = Resources.Load<InputReader>("InputReader");

        if (inputReader == null)
        { 
            inputReader = ScriptableObject.CreateInstance<InputReader>();
        }
        
        if (inputReader != null)
        {
            try
            {
                inputReader.Initialize();
                ServiceLocator.Register<InputReader>(inputReader);
                
            }
            catch (System.Exception e)
            { 
                inputReader = CreateFallbackInputReader();
                ServiceLocator.Register<InputReader>(inputReader);
            }
        }
        else
        { 
            inputReader = CreateFallbackInputReader();
            ServiceLocator.Register<InputReader>(inputReader);
        }

        yield return null;
    }

    /// <summary>
    /// ������� ������� ������ InputReader ��� �������, ����� �������� �� ��������
    /// </summary>
    private InputReader CreateFallbackInputReader()
    {
        var fallbackReader = ScriptableObject.CreateInstance<InputReader>();

        try
        {
            fallbackReader.Initialize();
           
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Even fallback InputReader failed to initialize: {e.Message}");
            // � ���� ������ ���� ����� �������� ��� ����� ��� � ���������� ������
        }

        return fallbackReader;
    }

    public void Cleanup()
    {
        if (inputReader != null)
        {
            try
            {
                inputReader.DisableAllInput();
                Debug.Log("InputSystem cleaned up");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error during InputSystem cleanup: {e.Message}");
            }
        }
    }

    /// <summary>
    /// ���������, ��������� �� ���������������� ������� �����
    /// </summary>
    public bool IsValid()
    {
        return inputReader != null;
    }

    /// <summary>
    /// �������� ������� InputReader
    /// </summary>
    public InputReader GetInputReader()
    {
        return inputReader;
    }
}