import time

from orange_network import OrangeNetwork

if __name__ == "__main__":
    model_path = r"C:\Users\Fran\Documents\Py\Citrus\Citrus_ft_optimized.tflite"
    input_path = r"C:\Users\Fran\Documents\Py\Agents\Agents\captures2"
    output_path = ""
    nn = OrangeNetwork(model_path, input_path, output_path)
    nn.start()
    while nn.is_alive():
        time.sleep(2)
    nn.stop()
