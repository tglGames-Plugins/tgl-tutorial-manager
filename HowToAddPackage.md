
## How to add this package?

### Adding as a github repository
- Open unity package manaegr
- On top right, there is a button to add a package
- add a git package (from git URL)
- fill the Https link for the package, in this case, 'https://github.com/tglGames-Plugins/tgl-tutorial-manager.git'
- Add
The package will be added under 'TGL Tutorial Manager' in packages, use as needed.

### Adding in scoped Registry

#### Add the toml file
You can add a toml file for accessing github from the computer or in the project and export it.

##### Add toml for the host machine
If the packages are to be used in a company development machine, where we need the user always accessible, we can add the toml file to the machine to avoid writing it every time.
the file needs to be stored in standard path:
- **Linux/macOS:** `~/.upmconfig.toml` (located in `/home/<your-user>/.upmconfig.toml`)
- **Windows:** `%USERPROFILE%\.upmconfig.toml` (located in `C:\Users\<your-user>\.upmconfig.toml`)

Assuming Linux, create `~/.upmconfig.toml` on the host machine:
```toml
[npmAuth."https://npm.pkg.github.com/@tglGames-Plugins"] 
token = "ghp_TESTERS_OWN_READ_PACKAGES_PAT"
email = "tester_email_id@domain"
alwaysAuth = true
```

##### Add toml for the project
If you are working on a personal project and need access to the scoped registry/package, add the toml to the project and not the whole machine:
Let's add the toml to the Assets folder of the project under a directory called `AccessConfig` named `upmconfig.toml`:
```toml
[npmAuth."https://npm.pkg.github.com/@tglGames-Plugins"]
token = "ghp_TESTERS_OWN_READ_PACKAGES_PAT"
email = "tester_email_id@domain"
alwaysAuth = true
```

As this project does not know where the custom toml file exists, we need to export it and then UPM will use this to read packages
so we open unity from terminal now:
```sh
$ export UPM_USER_CONFIG_FILE="/path/to/project/projectUPMRegistries.toml"
$ "/Path/To/Unity/Editor/Unity" -projectPath "/path/to/project"
```

so, if the project is at: `/home/thegamelearner/Documents/Unity Projects/TestTutorial/`
the toml file is at: `/home/thegamelearner/Documents/Unity Projects/TestTutorial/Assets/AccessConfig/upmconfig.toml`
and the unity we are using is at: `/home/thegamelearner/Unity/Hub/Editor/6000.2.6f2/Editor/Unity`

we will open Unity as :
```sh
$ export UPM_USER_CONFIG_FILE="/home/thegamelearner/Documents/Unity Projects/TestTutorial/Assets/AccessConfig/upmconfig.toml" 
$ 
$ "/home/thegamelearner/Unity/Hub/Editor/6000.2.6f2/Editor/Unity" -projectPath "/home/thegamelearner/Documents/Unity Projects/TestTutorial/"
```

#### Adding the registry
- Add the scoped registry in Unity setting: 
    - In 'Project Settings' window, on left side open 'Package Manager'
    - Add a new scoped registry(if not already added)
        - Name: `tglGames-Plugins`
        - Url: `https://npm.pkg.github.com/@tglGames-Plugins`
        - Scopes: `com.tglgames`

#### Add the package
You can use GUI (Unity editor) or manifest.json as needed.
**Add the package using GUI**
- Window -> Package Management -> Package Manager
- You may or may not change the registry on left panel to tglGames-Plugins
	- If you used something with search functionality, not github, you will see the package listed here.
	- In Unity, it shows "No items to display" if no other package from this scoped registry was added
- Use `Add(+)` on top left
- use "Install package by name"
- add the name and version
	- name: `com.tglgames.tgl-tutorial-manager`
	- version: `1.0.2` (Optional)
- Install
