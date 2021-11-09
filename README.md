## MultiplayerDynamicBonesMod

Makes dynamic bones multiplayer, allowing other players to interact with yours and vice versa, in addition to optimizing dynamic bones and more.

How to install? Follow the Automatic Installation instructions at https://melonwiki.xyz/#/README?id=installation-on-il2cpp-games
Then drop the mod dll in the Mods folder.  
**This mod requires [UIExpansionKit](https://github.com/knah/VRCMods)**   

   
Hotkeys | Description
 ---- | -----
F1 | Enable/disable the mod. You can also use the button in the quick menu if you're in VR or don't want to use the keyboard.
F4 | Dump a list of active players working with the mod, useful for debugging.
F5 | Open an ingame editor of Dynamic Bones settings if enabled in Mod Settings (Local only)
F8 | List what colliders are attached to your bones.
F9 | List colliders attached to other player's bones.
F11 | Prints list of all Avatars in current list

Once the mod has been run at least once, it creates a settings file in the vrchat folder under UserData, in a .cfg file. See the bottom of this document for a list of all settings. 



## **Screenshots!**
### **Toggle/Settings Menu**
From the 'MDB Settings' Button on the Quick Menu - A fast way to get to the most commonly adjusted settings  
_(The Quick Menu button can be disabled entirely by the 'Quick Menu Button' preference in Mod Settings)_  
![image](https://user-images.githubusercontent.com/68404726/112706831-e5578200-8e74-11eb-952a-38a6a74f329b.png)

### **MDB Avatar Config**
This Menu focuses on Customizing Dynamic Bone interaction with other avatars on a per avatar basis  
_This is on the Quick Menu when you select another Player or on the side of the big Avatar menu for your own avatar_    

* Include/Exclude a Specific Avatar - _This replaces the 'Enable/Disable MDB' buttons that used to be on the Per User Quick Menu_
	* Excluded avatars will not be multiplayered. 
	* Included avatars will bypass per avatar/player filters such as:
		*  'Only I can interact with other bones', 'Only friends and I can interact w/ eachother',  'Desktopers's colliders and bones won't be multiplayer'd'

* Include/Exclude Bones/Colliders lets you selectively enable or disable certain Dynamic Bones or Colliders from being multiplayered per avatar. 
	* Excluded Bones/Colliders will not be multiplayered. 
	* Included Bones/Colliders will bypass per bone/collider filters such as:
		* 'Only enable colliders in hands', 'Only the breast bones will be multiplayer'd'
* These checks can be disabled/enabled Globally with the 'Exclude Specific Objects' and 'Include Specific Objects' preferences. _Screenshot of this menu is below._ This is most useful for disabling an overly large collider on a model. 
* Adjusting an avatar's Dynamic Bone radius has several options. _Screenshot of this menu is below._
	* Multiply existing Bone Radius's by a Multiplier. 
		* Simply takes the existing bone radius and multiplies it by the multiplier you set. (Min x0.1) If bone is 0, it gets set to the fallback value in mod settings (Default 0.05)
	* Replace Bone Radius's
		* Replace all Dynamic Bone radius's with calculated values based on the bone length.
			* (The max value of either (bone.transform => bone.child(0).transform) or (m_Totalbonelength / Child Depth) then divided by the divisor set in Mod Settings and finally divided by the bone scale)
	* Exclude from 'Adjusting All Zero Radius Bones'
		* This only takes effect if you have the option 'Replace DB radius: Adjust All Zero Radius Bones' enabled in Mod Settings. This option will exclude the selected avatar from being adjusted by that global setting.
* Auto add hand colliders to this avatar
	* This toggles between 'Enabled/Disabled/NA' where Disabled also disables the global option for adding hand colliders for the selected avatar. 
	
![image](https://user-images.githubusercontent.com/68404726/112708152-6109fc80-8e7e-11eb-966f-7ed7ee4993e0.png)


### **Sub Menus**
![image](https://user-images.githubusercontent.com/68404726/112707256-01105780-8e78-11eb-9d89-dfc98d84392b.png)
### MoarBones
Because the world always needs more Dynamic Bones!    
This can be toggled in **Mod Settings**     
![image](https://user-images.githubusercontent.com/81715017/113239666-d0159580-9270-11eb-8010-0eddad1adeaa.png)     
"I hear you like bones~" Toggles this feature on/off     
"Performance Limit" Limits this to the first 10 avatars loaded since last toggle/world change     
"Don't effect local avatar" Stops moarbones from effecting you. Your own movements are networked through IK, so this is enabled by default.     
   
Or from **MDB Settings** on the Quick Menu     
![image](https://user-images.githubusercontent.com/81715017/113239574-9cd30680-9270-11eb-907f-9617b0535dfb.png)


## __Changelog:__

* Build 1043
	* Fixes for VRC 1149
	  * Changed around hooks and variables, resized menus to fit new UIX constraints
	* For settings changes that only affect one user, the mod now only unloads and reloads their bones, instead of everyone's
	* Optimized checks in loops, should be somewhat faster now when adding players
	* Fixed a bug in AutoAddHandColliders where it would fail if the fingers weren't the first children of the hands
	* Added an option to 'Visualize' avatar's DBs and DBCs, this is in the MDB menu when you select someone
	* Added an option for resetting 'DisableAllBones' to false on world change
	* Removed 'DisallowInsideColliders' from the MDB Quick Settings menu, that setting should always be enabled
* Build 1042.2
	* Fixes for VRC 1132
		* Updated methods for OnAvatarInstantiated, Reloading avatars after toggling Hand Colliders, Reloading all avatars after toggling Moar Bones.
	*  Changed the behavior of DistantDisable - This is now "Use **custom** value for disabling bones if beyond a distance"
		* This previous caused issues if the user disabled the option while bones were disabled for being a certain distance away. However it turns out this feature works differently than expected. 
			*   The default behavior is m_distanceToDisable = 10 m_distantDisable = True, but m_ReferenceObject = null. The DB docs say with a null refObj it will use the main camera instead. In my testing with MDB uninstalled this is correct, VRC natively disables distant (10m) away bones. We are switching to where m_DistantDisable is always true and we just change the value. This way when DistantDisable is true, we can use the user's value, be it smaller or larger, and when false, the native 10m.
		
* Build 1042
	* Fixes for VRC 1113
	  * Updated method for OnAvatarInstantiated
      * Removed some unneeded debugging
    * Misc null checks added for Hand&Breast filters and AddColliderToBone
	* Added a separate logging file option
* Build 1041.4
	* Fixes an error happens when using the Mod "Dynamic Bones Safety" and an NRE in ApplyBoneSettings that would happen when reloading all avatars. 
* Build 1041.3
	* VRC 1108 Compatibility - Fixed outdated method for reloading avatars and shouldn't break every other update now
* Build 1041.2
	* VRC 1106 Compatibility 
	* Fixed OnAvatarInstantiated Hook - Thanks loukylor for pointing at the right method
* Build 1041.1
	* Melonloader 0.4.0 Compatibility 
	* Adjusted the formatting in some debug messages 
	* Maybe fixed rare NRE caused by ApplyBoneSettings
* Build 1041
	* Added a 'Silent Mode' Log Level, this removes the join/leave/swap avatar messages from your log
	* Redid Hooks for RubyClient Compatibility - Thanks DubyaDude
	* Added the Loader Integrity Check
	* In game editor now waits a second between updates
* Build 1040.1-7
	* Minor bug fixes for various VRC versions
	* Added more description to MoarBones & MoarBones button is moar visible (Added to MDB Settings in Quick Menu)
	* Fix 'Disable All Bones' not really working if Enable if Visible was enabled
	* Fixed issue where all db and dbc were being added to the included list.
* Build 1040
	* Added an Include Specific option at the request of a mod user. 
		 * DynamicBones and Colliders now can toggle between Excluded/Included/NA.   
			* Excluded is never multiplayered, Included will bypass 'Only enable colliders in hands', 'Only the breast bones will be multiplayer'd'  
		 * Include/Exclude a Specific Avatar - _This replaces the 'Enable/Disable MDB' buttons that used to be on the Per User Quick Menu_  
			* Excluded avatars will not be multiplayered. Included avatars will bypass per avatar/player filters such as: 'Only I can interact with other bones', 'Only friends and I can interact w/ eachother',  'Desktopers's colliders and bones won't be multiplayer'd'  
	* Added a 'Disable All DynamicBones in Scene' option at the request of a ferret  
	* Hopefully fixed the scale checks for colliders - Lossy is the answer
	* Moved 'Multiply/Replace/Zero Radius Exclude' to it's own submenu  
	* Made Bone/Collider Specific menus go back to the previous one instead of close  

	* Added a new MoarBones feature, more info coming soon~   

	* Switched from storing lists in ModPrefs and moved to standalone files, on first load of this version the mod will migrate everything to files in UserData/MDB  
		* If there is ever an error loading a file, it will rename the old file and make a new one  
	* Various bug fixes and NRE fixes (onlyHandCollider & AddAutoCollidersToPlayer if model was human rigged, but missing hands)

* Build 1039
    * __Features/Changes:__
    	* Added toggles to enable an avatar's own Collider's to be added to their DynamicBones. Can be done just for you, and/or others. _Can be enabled in Mod Settings, or the Quick Menu MDB Settings_
    	* Added an option to auto add hand colliders for models that don't have them. Collider is based on hand size and there are options to enable per avatar, or for everyone. _Option for everyone is in Mod Settings, or the Quick Menu MDB Settings. Per avatar is in "MDB Avatar Config" on the Quick Menu when you select someone_.
    	* Added an option to scale an Avatar's DB's based on a multiplier, or replace the scales with a calculated value based on bone length  _"MDB Avatar Config" on the Quick Menu when you select someone_
	    	* Replace uses the following logic (for now)
		    	* Replaces all Dynamic Bone radiuses with calculated values based on the bone length. (The max value of either (bone.transform => bone.child(0).transform) or (m_Totalbonelength / Child Depth) then divided by the divisor set in Mod Settings and finally divided by the bone scale.)
	    	* Multiply simply takes the existing bone radius and multiplies it by the multiplier you set. If bone is 0, it gets set to the fallback value in mod settings (Default 0.05)
	    	* There also an option on Mod Settings to disable the adjustment of bones with a radius of 0
	    	* Added the option in Mod Settings "Replace DB radius: Adjust All Zero Radius Bones" This will replace 0 radius bones on all avatars. Can be disabled per avatar with the MDB Avatar Config on Quick Menu. 
    	* Added an option to completely disable the QuickMenu button
	    * Changed around Settings Quick Menu slightly, added a page 2
	    * Moved around locations of items on Mod Settings so they are grouped by category
	    * Added an autoupdate check back in
    * __Bugfix:__
    	* Added a null check in onlyHandColliders & breastsOnly if those db's or dbc's aren't really on a bone.
    	* Breast bone check now uses DynamicBone.m_Root.transform to account for when the script is placed on something other than the bone it is on.
    	* Collider size check now also includes local bone transform scale
    	* Ingame editor should apply bone setting changes now
    	    * Still needs more testing and need to make it not apply changes every frame if you are dragging the slider 

* Build 1038
	* Added a Quick Settings Button to replace the existing Toggle (Can disable with preference and revert the button back to just a toggle)
	* Changed avatarsToExclude to use a combo of Avatar Name+AvatarID. Same for excludeSpecific db and dbc 
		* This is a breaking change for existing lists but should remove the possibility for collisions in the ID 
	* Reworked and cleaned up the filters/checks for db and dbc interaction. The following were not working and have been fixed:
		* Only friends and I can interact w/ eachother
		* Only enable colliders in hands
		* Desktopers's colliders and bones won't be multiplayer'd
		* Only the breast bones will be multiplayer'd - This found anything that was a child of the chest, included head/arms.
	* Fixed Print excluded colliders was repeating excluded db list.
	* Fixed Typo for AvatarMenu Exclude Bones button
	* Removed unused and commented out code
* Build 1037
	* Minor changes for the most part.
		* Cleaned up logging and added a debug logging preference.
		* Removed/Commented out unused code.
		* Added sorting to Printing Excluded DynamicBones and DynamicBoneColliders.
		* Changed the final check in SkinnedmeshRender search for visibility.
		* Added a preference to disable adjusting dynamic bones properties along with the update rate.
		* Fixed a bug causing the collider exclude menu to not show current state of enabled/disabled.
		* Remade the EnableUserPanelButton-AvatarsExclude preference so it defaults to on for existing users. 
			* Did this because the button was broke in the past.
* Build 1036
	* Minor spelling and consistency changes: 
		* Made some settings names shorter to better fit on the quick menu
		* Enable/Disable "Dynamic Bones mod" Toggle text is now same no matter the button state
		* Changed the Quick Menu Enable/Disable per avatar into two buttons since you were unable to tell the Toggle state of the previous button without clicking it and thus changing it 
	* Implemented avatar blocking via the quick menu, this button did nothing previously 
		* Enabling/Disabling the avatar on the quick menu now records the avatar name in AvatarsToWhichNotApply 
			* QuickMenu.prop_QuickMenu_0.prop_APIUser_0.avatarId – was returning nothing
			* Replaced with - QuickMenu.prop_QuickMenu_0.field_Private_Player_0.prop_ApiAvatar_0.name
		* enableUserPanelButton was set to "OnlyDynamicBonesOnBreasts" and not "EnableUserPanelButton"
		* Enabling/Disabling a person's avatar now toggles the mod off and on if MDB is enabled 
			* This is so the avatar unloads/loads immediately
	* in AddCollidersToAllPlayers, changed ApplyBoneSettings to use the variable name 'bone' instead of 'collider' since item3 is a DynamicBone
	* Fixed m_UpdateRate not applying. 
		* Also now changing Elasticity, Stiffness, Damping, Inert based on a ratio from the inital setting of the bone. 
			* Kinda works for helping compensate, but needs more work 
	* EnableIfVisible was not working with some Avatars. AddDynamicBonesToVisibilityList was finding the first SkinnedMeshRenderer on the model, which wasn't necessarily the body. If this mesh render wasn't enabled, the avatar's dynamic bones were always disabled. 
		* Mod now checks for an active "Body" mesh first and if it doesn't find one, it uses largest active Mesh, if that finds nothing, falls back to the first MeshRender. 
	* Fixes bug with bone visibility enabling bones that were disabled by default, they are now removed from the visibility list. 
	* Fixed the collider radius and height checks in AddColliderToBone. This now multiplies these values with the Armature scale
	* Fully implemented a feature to exclude specific dynamic bones or colliders from an avatar, this can be done in game with a custom menu "MDB Specific Exclude" using UIExpansionKit. Buttons appear for this on the Avatar big menu and other user quick menu.
		* There is an extra option in modprefs 'excludeSpecificBones' these bone names will be excluded on all avatars.
		* __UIExpansionKit is now required__

__Known bugs:__
* If Mod is disabled and you save mod settings, the button on the menu will switch to 'Press to disable DBM' despite the mod being disabled. Toggle the button on and off will resync it's state.

__Todo:__ 
* Look over the ingame editor.
    * Still needs more testing and need to make it not apply changes every frame if you are dragging the slider - Now is every second
* Make (require restart) options update live
* In game visulzation of db and dbcs 

## **Settings**
Setting Name | Default Value | Display Text | Extra info
------------ | ------------- | ------------- | -------------
__What gets Mutliplayered__ | - | - | -
EnabledByDefault | true | Enabled by default | If mod is enabled when game starts
OptimizeOnly | false | Optimize bones, don't enable interaction **[QM]** | Options such as DistanceDisable, EnableJustIfVisible and, DynamicBoneUpdateRate will still apply to help optimize dynamic bones. **I recommend using this when you don't want to interact with others instead of disabling the mod completely**
OnlyMe | false | Only I can interact with other bones **[QM]**
OnlyFriends | false | Only friends and I can interact w/ eachothers bones **[QM]**
DisallowDesktoppers | false | Desktopers's colliders and bones won't be multiplayer'd **[QM]**
OnlyHandColliders | false | Only enable colliders in hands **[QM]**
OnlyDynamicBonesOnBreasts | false | Only the breast bones will be multiplayer'd **[QM]** | 'Breast bones' is defined as anything attached to the chest and not a child of the Left/Right Shoulder or Neck. 
InteractSelf | false | Add your colliders to your own bones (May cause buggy interactions) **[QM]**
OthersInteractSelf | false | Add other avatar's colliders to their own bones (May cause buggy interactions) **[QM]**
AddAutoCollidersAll | false | Auto add hand colliders to avatars that don't have them (Requires reload of avatar) **[QM]** | If this setting is enabled, you can disable this behavior per avatar by selecting them and opening the 'MDB Avatar Config' on your QuickMenu. Toggle the hand collider option on that menu to 'Disabled'
ExcludeSpecificBones | true  | Exclude Specific Bones from being Multiplayered **[QM]** | If the bones/colliders set in the per avatar exclude menus wont be multiplayered
IncludeSpecificBones | true | Include Specific Bones or Colliders to be Multiplayered[QM] | If the bones/colliders set in the per avatar include menus will bypass filters 
__Bone settings__ | - | -
DistanceDisable | true | Custom value for disabling bones if beyond a distance **[QM]**
DistanceToDisable | 4 | Distance limit | For above setting, in meters
DisallowInsideColliders | true | Disallow inside colliders from being multiplayered **[QM]**
DestroyInsideColliders | false | Destroy inside colliders (Requires reload of avatar) **[QM]**
ColliderSizeLimit | 1f | Collider size limit
DynamicBoneUpdateRate | 60 | Dynamic bone update rate
DynamicBoneUpdateRateAdjSettings | true | Adjust bone properties in a ratio from update rate change
EnableJustIfVisible | true | Enable dynamic bones only if they are in view | Uses the visibility of an avatar's skinned mesh render to check for view
VisibilityUpdateRate | 1f | Visibility update rate (seconds)
BoneRadiusDivisor | 4 | Replace DB radius: Divisor - New Radius is BoneLegnth / ThisValue
EndBoneRadius | 0.05f | Replace DB radius: This is the fallback radius if the calculated value is 0
AdjustRadiusExcludeZero | false | Replace DB radius: Excludes bone with a radius of 0 from being changed
AdjustRadiusForAllZeroBones | false | Replace DB radius: Adjust All Zero Radius Bones - Replace the radius for all bones with a radius of 0 on all avatars
DisableAllBones | false | Disable all Dynamic Bones in Scene **[QM]**
__Mod settings__ | - | -
KeybindsEnabled | true | Enable keyboard actuation(F1, F4 and F8) | See above for bindings
UpdateMode | 2 | A value of 2 will notify the user when a new version of the mod is available, while 1 will not.
EnableEditor | false | EnableEditor (F5) | GUI for live editing of bones, may need some work? 
QuickMenuButton | 1 | Quick Menu Button - 1:Settings Menu, 2:Just Toggle, 0:None (Restart Req) | Controls the state of the button that gets placed on your Quick Menu
LogLevel | 0 | Console Logging Level: 0-Default, 1-Info, 2-Debug, 3-ExtraDebug(Very laggy), -1 Silent Mode | Logging info if you really want it
MoarBones | false | MoarBones: I hear you like bones~
MoarBonesPref | true | MoarBones: Performance Limit
MoarBonesNotLocal | true | MoarBones: Don't effect local avatar | Enabled by default as the local avatar movements are networked
__Hidden Lists/Text files__ | - | -
AvatarsToAddColliders.txt | null |_Doesn't show in settings_ | One line per avatar, this is populated through the in game menus. AvatarName+Hash
AvatarsToAdjustDBRadius.txt | null |_Doesn't show in settings_ | One line per avatar, this is populated through the in game menus. "AvatarName+Hash, 0.1-∞\|0\|-2",  0.1-∞ = Multiply to scale bones radius's by, 0 = Replace with Calculated, -2 = 'Exclude from Adjusting All Zero Radius Bones'
AvatarsToWhichNotApply.txt | null |_Doesn't show in settings_ | One line per avatar, this is populated through the in game menus. "AvatarName+Hash, True\|False" True = Exclude, False = Include
BonesToAlwaysExclude.txt | null |_Doesn't show in settings_ | One line per Bone, user exact bone names **This must be manually populated in the config file** Example: (, are line breaks) Left Z_Wing_Bone_3, Left Z_Wing_Bone_2, Left Z_Wing_Bone_1, Right Z_Wing_Bone_3, Right Z_Wing_Bone_2, Right Z_Wing_Bone_1
BonesToExclude.txt | null |_Doesn't show in settings_ | One line per Bone, this is populated through the in game menus. AvatarName+Hash:db:BoneName
BonesToInclude.txt | null |_Doesn't show in settings_ | One line per Bone, this is populated through the in game menus. AvatarName+Hash:db:BoneName
CollidersToExclude.txt | null |_Doesn't show in settings_ | One line per Collider, this is populated through the in game menus. AvatarName+Hash:dbc:ColliderName
CollidersToInclude.txt | null |_Doesn't show in settings_ | One line per Collider, this is populated through the in game menus. AvatarName+Hash:dbc:ColliderName


**[QM]** means the setting is on the QuickMenu Settings Menu
