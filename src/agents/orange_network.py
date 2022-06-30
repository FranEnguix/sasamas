import os
import numpy as np
import shutil
import tensorflow as tf

from datetime import datetime, timedelta
from threading import Thread, Event

class OrangeNetwork(object):

    def __init__(self, model_path:str, input_path:str, output_path:str):
        self.__interpreter: tf.lite.Interpreter = tf.lite.Interpreter(model_path)
        self.__interpreter.allocate_tensors()
        self.__class_names: list[str] = ["black spot", "canker", "greening", "healthy", "scab"]
        self.input_path: str = input_path
        self.output_path: str = output_path
        self.__alive: bool = False
        self.__last_image: datetime = datetime.min
        self.prepare_input_folder()
        self.prepare_output_folder()

    def start(self):
        self.prepare_output_folder()
        self.__exit_event: Event = Event()
        self.__alive = True
        self.__thread: Thread = Thread(target=self.launch_network)
        self.__thread.daemon = True
        self.__thread.start()

    def stop(self):
        self.__alive = False
        self.__exit_event.set()

    def is_alive(self):
        return self.__alive

    def prepare_output_folder(self):
        if (not os.path.isdir(self.output_path)):
            os.makedirs(self.output_path)

    def prepare_input_folder(self):
        if (not os.path.isdir(self.input_path)):
            os.makedirs(self.input_path)

    def launch_network(self):
        input_details = self.__interpreter.get_input_details()
        while self.__alive:
            files = self.get_non_processed_files()
            if not files:
                self.__alive = False
            for image_path in files:
                if self.__exit_event.is_set():
                    self.__alive = False
                    break
                t0 = datetime.now()
                img_tensor = self.image_to_tensor(image_path)
                highest_prediction = self.get_network_prediction(img_tensor, input_details)
                td = datetime.now() - t0
                self.__last_image = datetime.fromtimestamp(os.path.getmtime(image_path))
                self.process_output(image_path, td, highest_prediction)

    def process_output(self, image_path:str, time_delta: timedelta, highest_prediction: int) -> None:
        with open(f"{self.output_path}/log.txt", "a") as f:
            f.write(f"Image {image_path} processed in {time_delta.total_seconds():.2f}s. Output: {self.__class_names[highest_prediction]}\n")
        image_filename = os.path.basename(image_path)
        shutil.copyfile(image_path, f"{self.output_path}/{image_filename}")

    def get_network_prediction(self, tensor, input_details) -> np.intp:
        self.__interpreter.set_tensor(input_details[0]['index'], tensor)
        self.__interpreter.invoke()
        output_details = self.__interpreter.get_output_details()
        outputs = self.__interpreter.get_tensor(output_details[0]['index'])
        pred = np.squeeze(outputs)
        return np.argmax(pred)

    def image_to_tensor(self, image_path: str):
        input_image = tf.keras.utils.load_img(image_path, target_size=(224, 224))
        img_tensor = tf.keras.utils.img_to_array(input_image)
        img_tensor = np.expand_dims(img_tensor, axis=0)
        img_tensor /= 255. # Normalize
        return img_tensor

    def get_non_processed_files(self) -> list:
        files = [os.path.join(self.input_path, f) for f in os.listdir(self.input_path) \
                if datetime.fromtimestamp(os.path.getmtime(f"{self.input_path}/{f}")) > self.__last_image]
        if files:
            files.sort(key = lambda x: os.path.getmtime(x))
        return files




