**Framework Vishnu**

A powerful Unity framework for creating and managing sequential and non-sequential tasks, designed to simplify task workflows and enhance modularity in Unity projects.

Features

	•	🚀 Task Management: Effortlessly create and manage sequential or non-sequential tasks.
	•	📦 Lightweight & Modular: Designed for flexibility and easy integration into any Unity project.
	•	🎯 Sample Scene Included: Demonstrates how to use the framework with a functional example.
	•	💻 Editor Integration: Simplifies task setup and management with user-friendly interfaces.

Installation

Option 1: Add via Git URL

	1.	Open your Unity project.
	2.	Navigate to Window > Package Manager.
	3.	Click the + button and select Add package from git URL….
	4.	Paste the following URL:

https://github.com/BrusleyM/Framework-package.git



Option 2: Manual Installation

	1.	Clone the repository:

git clone https://github.com/BrusleyM/Framework-package.git


	2.	Copy the package folder into your Unity project’s Packages directory.

Getting Started

Import the Sample Scene

	1.	Open Window > Package Manager.
	2.	Select Framework Vishnu from the list of packages.
	3.	Under Samples, click Import next to the “Test Scene” sample.

The sample will be added to your project at:

Assets/Samples/Framework Vishnu/<version>/Scenes/Test scene.unity

Quick Start Guide

	1.	Set up a Task Manager:
	•	Create a new GameObject and attach the TaskManager script.
	2.	Define Tasks:
	•	Create new task scripts inheriting from BaseTask.
	•	Implement task-specific logic by overriding relevant methods (StartTask, CompleteTask, etc.).
	3.	Add Tasks to Task Manager:
	•	Populate the TaskManager with your custom tasks in the Unity Inspector.

Example

Here’s an example of a simple custom task:

```
using Framework.Tasks;
public class ExampleTask : BaseTask
{
    public override void StartTask()
    {
        Debug.Log("Task started!");
        CompleteTask();
    }
    public override void CompleteTask()
    {
        Debug.Log("Task completed!");
        base.CompleteTask();
    }
}
```

Documentation

Core Components

	•	BaseTask: The foundation for all tasks. Extend this class to create custom tasks.
	•	TaskManager: Manages the execution and flow of tasks.
	•	Utilities: Includes helper functions for common task-related operations.

Samples

The package includes a sample scene to demonstrate:
	•	Task setup and execution.
	•	Sequential and non-sequential task flows.

Contributing

We welcome contributions to improve this framework! To contribute:
	1.	Fork the repository.
	2.	Create a new branch for your feature or fix.
	3.	Submit a pull request with a detailed description.

Known Issues

	•	None at the moment. Please report issues via GitHub Issues.

Contact

For questions, feedback, or support:
	•	Author: Brusley Masemola
	•	Email: brusleymasemola@hotmail.com
	•	GitHub: BrusleyM
