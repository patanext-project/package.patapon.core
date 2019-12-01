**NOTE: Current contributions are not accepted until the refactoring is complete**
**NOTE: This file is not yet finished**

For contributing new features to the game, please check the project Trello to see which feature you can develop.

# Guidelines
Please check https://docs.microsoft.com/fr-fr/dotnet/standard/design-guidelines/    
It's possible that the project iself don't follow some of the guideline, if  that the case, report an issue.
(It may also be possible that there are some exceptions, that I'll need to add later here)

- For Unity files (MonoBehaviour based or serialized struct/classes), use a pascal-case naming.
- Static fields (and auto properties) shouldn't be used too much (If you wish to store properties, use configuration components in respective worlds)
- Do not create any MonoBehaviour files, except when used for Presentation/Backend gameobjects. 

# Running/Building this project
Right now, it's kinda hard to run this project, you'll need to copy quite a lot of my other repositories, I'm currently working on a simpler solution where you'll just be able to write a line of text in an empty Unity project to run this project.

### todo...