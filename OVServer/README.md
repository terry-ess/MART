# OpenVINO Object Detection Server

The server is a Python 3.8 application that implements a simple UDP/IP "server" that uses Intel's OpenVINO for object detection inference.  This is an alternative to a TensorFlow based inference server.  
Testing has shown a significant improvement in performance using OpenVINO rather then TensorFlow inference. This requires the installation of [OpenVINO 2021 4.1 LTS](https://www.intel.com/content/www/us/en/developer/tools/openvino-toolkit/overview.html) and [OpenCV](https://pypi.org/project/opencv-python/). 

Supported commands:

- Are you there? - hello
- Shutdown - exit
- Run an inference -  model name, full path to image, min. score, object ID

Three of the models used in this application are [based on TensorFlows's ssd_mobilenet_v1](http://download.tensorflow.org/models/object_detection/ssd_mobilenet_v1_coco_2018_01_28.tar.gz).  The people model is just the coco trained model converted by the OpenVINO tool set.  The hand and hand-robot arm models are transfer learning trained versions of it also converted by the OpenVINO tool set.  The face model uses an [Intel provided face recognition model](https://github.com/openvinotoolkit/open_model_zoo/tree/releases/2021/4/models/intel/face-detection-adas-0001). 
