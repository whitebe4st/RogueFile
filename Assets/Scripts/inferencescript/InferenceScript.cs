using UnityEngine;
using Unity.InferenceEngine;
 // Use Unity.InferenceEngine if on a very new 2026 build

public class SimpleInference : MonoBehaviour
{
    public ModelAsset modelAsset; // Drag your .sentis or .onnx file here
    public Texture2D inputImage;  // Drag a test image here
    
    private Model runtimeModel;
    private Worker worker;

    void Start()
    {
        // 1. Load the model into a format Sentis understands
        runtimeModel = ModelLoader.Load(modelAsset);

        // 2. Create the Worker (the engine). 
        // GPUCompute is fastest for vision models.
        worker = new Worker(runtimeModel, BackendType.GPUCompute);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) // Press Space to run
        {
            ExecuteInference();
        }
    }

    void ExecuteInference()
    {
        // 3. Convert your texture into a Tensor (the format the AI needs)
        // Note: Check your model's expected size (usually 224x224 or 256x256)
        using Tensor<float> inputTensor = new Tensor<float>(new TensorShape(1, 3, 224, 224));
        TextureConverter.ToTensor(inputImage, inputTensor);

        // 4. Run the model
        worker.Schedule(inputTensor);

        // 5. Peek at the results (without downloading from GPU yet)
        Tensor<float> outputTensor = worker.PeekOutput() as Tensor<float>;

        // 6. To see the numbers, move data to CPU (Warning: this can be slow)
        using var cpuTensor = outputTensor.ReadbackAndClone();
        float[] results = cpuTensor.DownloadToArray();
        Debug.Log("Inference complete! First value: " + results[0]);
    }

    private void OnDisable()
    {
        // 7. Cleanup is MANDATORY to avoid memory leaks/crashes
        worker?.Dispose();
    }
}