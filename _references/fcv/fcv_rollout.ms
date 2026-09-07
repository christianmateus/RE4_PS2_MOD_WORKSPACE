/*	======================================================================
	
	Maxscript:    [PC] Resident Evil 4
	Author:        mariokart64n
	Date:           January 14 2022
	
	""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""
	
	Description:
		UI for FCV Editing
	
	Change Log:
		2023-07-12
		  - fixes scaling and axis swap for camera
		  - problem with gimbal lock causing animation glitches
		  - figured out the other vales in the FCV are tagenets,
		    but unsure how to apply them.
			
		2023-07-09
		  - discard the first 4 bits for data type now due to Biohazard4X's
		    discover about the IK pole flag.
		  - added a apply morph in the right click to test some of the face
		    morph key frames
		
		2022-14-01
		  - Wrote It!
	
	======================================================================	*/	
clearListener()
-- include "C:\\Users\\Corey\\OneDrive\\Private\\RE4_Toolset\\formats\\fcv_format.ms"

try(destroyDialog ui_fcv)catch(ui_fcv)
rollout ui_fcv "FCV Frame Curve Values" (
	group "Manage" (
		button btn1 "Open" width:64 across:2
		button btn2 "Save" width:64
		label lbl_file "<NO FILE SELECTED>" align:#center
		label lbl1 "Timeline:" width:50 align:#left across:2
		spinner spn1 "" fieldWidth:60 align:#right type:#integer range:[0, 65535, 0]
		editText edt_bone_prefix "Node Prefix:" align:#left text:"bone"
		listBox lbx1 "" height:10 across:2
		listBox lbx2 "" height:10
		label lbla "Object" align:#left across:8
		label lblb ""
		button btn_t_add "+" width:16 height:16
		button btn_t_sub "-" width:16 height:16
		label lblc "Frame" align:#left
		label lbld ""
		button btn_k_add "+" width:16 height:16
		button btn_k_sub "-" width:16 height:16
		label lbl_bonename "                                    " align:#center
		spinner spn2 "" fieldWidth:56 align:#left type:#integer range:[0,255,0] across:2
		spinner spn3 "" type:#integer range:[0, 65535, 0] fieldWidth:56 align:#right
		label lbl3 "Format:" width:45 align:#left across:2
		dropdownlist ddl1 "" align:#right width:90 toolTip:"f = 32bit Float Point"
		label lbl4 "Type:" width:45 align:#left across:2
		dropdownlist ddl2 "" align:#right width:90 toolTip:"M = Move\nR = Rotate"
		listBox lbx3 "" height:3
		checkbox chk1 "Enable"
		label lbl6 "Value1:" align:#left across:2
		editText edt1 "" width:80 align:#right enabled:false
		label lbl7 "Value2:" align:#left across:2
		editText edt2 "" width:80 align:#right enabled:false
		label lbl8 "Value3:" align:#left across:2
		editText edt3 "" width:80 align:#right enabled:false
		label lbl9 "Value4:" align:#left across:2
		editText edt4 "" width:80 align:#right enabled:false
		)
	group "Export" (
-- 		button btn_idle_pose "Get Idle Pose"
		button btn_export "Copy Keys to FCV" width:100
		checkbox chk_exp_rot "Include Rotations" checked:true
		checkbox chk_exp_pos "Include Positions" checked:false
		checkbox chk_exp_merge "Merge" checked:false
		checkbox chk_exp_cull "Reduce Keyframes" checked:false
		checkbox chk_exp_old "Old Export Function" checked:false
		spinner spn_exp_scale "Scale:" type:#float range:[0.0, 10000000.0, 1.0] align:#center
		)
	group "Helpers" (
		dropdownlist ddl_bone_set "BoneSet:" items:#("pl00") default:1
		button btn_link_bone "Link" width:50 across:2
		button btn_sel_const "Select" width:50
		)

	struct fmtFCV_Config (
		
		type = 0,
		/*			76543210
			-------------------------------------
		
			? does nothing ?
				0x00 00000000 
				0x01 00000001 
			
			Type of transformation
				0x02 00000010 Rotation
				0x04 00000100 Movement
				0x08 00001000 Scale
			
			? Rest may be for inverse kinetmatics ?
				0x10 00010000 
				0x20 00100000 
				0x40 01000000 
				0x80 10000000 
		*/
		data = 0,
			/*
			0x00 = #(float, float, float)
			0x10 = #(int16_t, int16_t, int16_t, int16_t)
			0x20 = #(int16_t, int16_t, int16_t)
			0x50 = #(int16_t, int16_t, int16_t) -- to get degrees = (int16_t / 65535.0) * 360.0
			0x54 = #(int16_t, int16_t, int16_t)
			0x60 = #(int16_t, int16_t)
			0x64 = #(int16_t, int16_t)
			0xA0 = #(int8_t)
			
			the first 4 bits is the read type, the last 4 bits are related to the compass direction of the IK pole
			0 = Left?
			    1, 2, 3 apparently are not used?
			4 = Forward?
			5 = Backwards?
			6 = Right?
			
			*/
		fn read &f = (
			type = readByte f #unsigned
			data = readByte f #unsigned
			),
		fn write &s = (
			writeByte s type #unsigned
			writeByte s data #unsigned
			)
		)

	struct fmtFCV_Value ( 
		num_keys = 0,
		keys = #(),
		values = #(),
		/*
			values may all fit into the same 3 fields:
			1.) Value
			2.) Incoming Tangent
			3.) Outgoing Tangent (value needs to be inverted for 3dsmax)
		*/
		fn block_size &data_type = (
			num_keys = if keys.count < values.count then keys.count else values.count
			local bs = 2 + num_keys + num_keys
			case (bit.shift data_type -4) of (
				0x00: (bs += 12 * num_keys)
				0x01: (bs += 8 * num_keys)
				0x02: (bs += 6 * num_keys)
				0x04: (bs += 10 * num_keys)
				0x05: (bs += 6 * num_keys)
				0x06: (bs += 4 * num_keys)
				0x08: (bs += 9 * num_keys)
				0x09: (bs += 5 * num_keys)
				0x0A: (bs += 3 * num_keys)
				0x0F: (bs += 4 * num_keys)
				)
			bs
			),
		fn read &f &data_type = (
			if (num_keys = readShort f #unsigned) > 0 do (
				local i = 1
				keys[num_keys] = 0
				for i = 1 to num_keys do (keys[i] = readShort f #unsigned)
				values[num_keys] = #(0.0, 0.0, 0.0, 0.0)
				for i = 1 to num_keys do (
					values[i] = #(0.0, 0.0, 0.0, 0.0)
					case (bit.shift data_type -4) of (
						0x00: (values[i] = #(readfloat f, readfloat f, readfloat f, 0)) 												-- Float:Float
						0x01: (values[i] = #(readFloat f, readShort f #signed, readShort f #signed, 0)) 								-- Float:Short
						
						0x02: (values[i] = #(readShort f #signed, readShort f #signed, readShort f #signed, 0)) 						-- Short:Short
						0x04: (values[i] = #(readShort f #signed, readFloat f, readFloat f, 0)) 										-- Short:Float
						
						0x05: (values[i] = #(readShort f #signed, readShort f #signed, readShort f #unsigned, 0))						-- Short:Short
						0x06: (values[i] = #(readShort f #signed, readbyte f #signed, readbyte f #signed, 0)) 							-- Short:Byte
						
						0x08: (values[i] = #(readByte f #signed, readFloat f, readFloat f))												-- Byte :Float
						0x09: (values[i] = #(readbyte f #signed, readShort f #signed, readShort f #signed, 0))							-- Byte :Short
				
						0x0A: (values[i] = #(readbyte f #signed, readbyte f #signed, readbyte f #signed, 0))							-- Byte :Byte
						0x0F: (values[i] = #(readbyte f #signed, readbyte f #unsigned, readbyte f #unsigned, readbyte f #unsigned))		-- Byte :??
						default: (exit)
						)
					)
				)
			),
		fn write &s &data_type = (
			if (num_keys = if keys.count < values.count then keys.count else values.count) > 0 do (
				writeShort s num_keys #unsigned
				local i = 1
				for i = 1 to num_keys do (writeShort s keys[i] #unsigned)
				for i = 1 to num_keys do (
					case (bit.shift data_type -4) of (
						0x00: (
							writeFloat s values[i][1]
							writeFloat s values[i][2]
							writeFloat s values[i][3]
							)
						0x01: (
							writeFloat s values[i][1]
							writeShort s values[i][2] #signed
							writeShort s values[i][3] #signed
							)
						0x02: (
							writeShort s values[i][1] #signed
							writeShort s values[i][2] #signed
							writeShort s values[i][3] #signed
							)
						0x04: (
							writeShort s values[i][1] #signed
							writeFloat s values[i][2]
							writeFloat s values[i][3]
							)
						0x05: (
							writeShort s values[i][1] #signed
							writeShort s values[i][2] #signed
							writeShort s values[i][3] #signed
							)
						0x06: (
							writeShort s values[i][1] #signed
							writeByte s values[i][2] #signed
							writeByte s values[i][3] #signed
							)
						0x08: (
							writeByte s values[i][1] #signed
							writeFloat s values[i][2]
							writeFloat s values[i][3]
							)
						0x09: (
							writeByte s values[i][1] #signed
							writeShort s values[i][2] #signed
							writeShort s values[i][3] #signed
							)
						0x0A: (
							writeByte s values[i][1] #signed
							writeByte s values[i][2] #signed
							writeByte s values[i][3] #signed
							)
						0x0F: (
							writeByte s values[i][1] #signed
							writeByte s values[i][2] #unsigned
							writeByte s values[i][3] #unsigned
							writeByte s values[i][4] #unsigned
							)
						default: (
							format "Format Not Supported [%]\n" data_type
							exit
							)
						)
					)
				)
			)
		)

	struct fmtFCV_Track (
		index = 0, -- not apart of the format spec, i just store the block order here
		x = fmtFCV_Value(),
		y = fmtFCV_Value(),
		z = fmtFCV_Value(),
		fn read &f &data_type = (
			x.read f data_type
			y.read f data_type
			z.read f data_type
			),
		fn block_size &data_type = (
			local bs = 0
			bs += x.block_size data_type
			bs += y.block_size data_type
			bs += z.block_size data_type
			bs
			),
		fn write &s &data_type = (
			x.write &s data_type
			y.write &s data_type
			z.write &s data_type
			)
		)

	struct fmtFCV (
		num_frames = 0,
		num_tracks = 0,
		config = #(),
		nodes = #(), -- buffer is padded to an alignment of 4
		filesize = 0, -- excluding padding at the end of the file, number is in big endian (null in some files)
		addrs = #(), -- address to each track (address is absolute, from the start of file)
		tracks = #(),
		-- file is padded to byte alignment of 32, pad character is 0xCD
		fn calcBlockEnds &fsize &addrs = (
			local i = 1
			local limits = #()
			local index = 0
			for i = 1 to addrs.count do (
				if (index = findItem limits addrs[i]) == 0 do (
					append limits addrs[i]
					)
				)
			append limits filesize
			sort limits
			limits
			),
		fn read file = (
			local f = try(fopen file "rbS")catch(undefined)
			
			if f != undefined then (
				num_frames = readShort f #unsigned
				if (num_tracks = readByte f #unsigned) > 0 do (
					config[num_tracks] = fmtFCV_Config()
					tracks[num_tracks] = fmtFCV_Track()
					nodes[num_tracks] = 0
					addrs[num_tracks] = 0
					local i = 1
					for i = 1 to num_tracks do (
						config[i] = fmtFCV_Config()
						config[i].read f
						)
					for i = 1 to num_tracks do (nodes[i] = readByte f #unsigned)
					fseek f (mod (4-(mod (ftell f) 4)) 4) #seek_cur
					filesize = bit.swapBytes (bit.swapBytes (readLong f #unsigned) 1 4) 2 3
					for i = 1 to num_tracks do (addrs[i] = readLong f #unsigned)
					
					-- preserve block order
					local tmp = copy addrs #nomap
					sort tmp
					
					for i = 1 to num_tracks do (
						fseek f addrs[i] #seek_set
						--format "Track%:\t0x%\t%\n" i (bit.IntAsHex(addrs[i] as integer)) (config[i] as string)
						tracks[i] = fmtFCV_Track()
						tracks[i].read f config[i].data
						tracks[i].index = findItem tmp addrs[i] -- create block order
						)
					
					)
				fclose f
				) else (format "failed to open file\n")
			),
		fn write file = (
			local s = try(fopen file "wb")catch(undefined)
			if s != undefined then (
				num_tracks = if config.count < nodes.count then config.count else nodes.count
				num_tracks = if tracks.count < num_tracks then tracks.count else num_tracks
				if num_tracks > 0 do (
					local i = 1
					writeShort s num_frames #unsigned
					writeByte s num_tracks #unsigned
					for i = 1 to num_tracks do (config[i].write &s)
					for i = 1 to num_tracks do (writeByte s nodes[i] #unsigned)
					local ptr = (num_tracks * 3) + 3
					for i = 1 to (mod (4-(mod ptr 4)) 4) do (writeByte s 0)
					ptr = ftell s + 4 + (num_tracks * 4)
					filesize = ptr
					for i = 1 to num_tracks do (filesize += tracks[i].block_size config[i].data)
					local padding = mod (32-(mod filesize 32)) 32
					filesize += padding
					writeLong s (bit.swapBytes (bit.swapBytes filesize 1 4) 2 3) #unsigned
					/*
					edited to write the blocks in a preserved order
					*/
					local order = #()
					local order_sorted = #()
					order[num_tracks] = 0
					for i = 1 to num_tracks do (order[i] = tracks[i].index)
					local order_sorted = copy order #nomap
					sort order_sorted
					local x = 0
					addrs = #()
					addrs[num_tracks] = 0
					for i = 1 to num_tracks do (
						x = findItem order order_sorted[i]
						addrs[x] = ptr
						ptr += tracks[x].block_size config[x].data
						)
					for i = 1 to num_tracks do (writeLong s addrs[i] #unsigned)
					for i = 1 to num_tracks do (
						x = findItem order order_sorted[i]
						tracks[x].write &s config[x].data
						)
					for i = 1 to padding do (writebyte s 0xCD #unsigned) -- pad to 32
					)
				fclose s
				) else (format "failed to save file\n")
			),
		fn getBonesFromSelection &boneArray &boneIdArray bname_prefix:"bone" = (
			-- Get Bone Selection
			boneArray = #()
			boneIdArray = #()
			local o = undefined
			local sel = selection as array
			local i = 1
			local c = UndefinedClass
			if sel.count == 0 do sel = objects as array
			for o in sel where ((c = classof o) == boneGeometry or c == Dummy) do (
				-- check that the bone is a index, not a name
				if matchpattern o.name pattern:(bname_prefix + "_*") do (
					-- split name
					i = try((subString o.name ((findString o.name "_") + 1) -1) as integer)catch(-1)
					if i > -1 do (
						append boneIdArray i
						append boneArray o
						)
					)
				)
			),
		fn getNodeIndex nodeid typeid = (
			local i = 1
			local r = 0
			for i = 1 to nodes.count do (
				if nodes[i] == nodeid and config[i].type == typeid do (
					r = i
					exit
					)
				)
			r
			),
		fn getFromScene doRot:true doPos:false doMerge:false cullEmpty:true mscale:100.0 bname_prefix:"bone" = ( -- mscale is set to 100.0 to match son of persia's import scale
			local boneArray = #()
			local boneIdArray = #()
			getBonesFromSelection &boneArray &boneIdArray bname_prefix:bname_prefix
			if boneArray.count > 0 do (
				if not doMerge do (
					nodes = #() -- kills previous data
					config = #()
					tracks = #()
					)
				num_frames = (animationrange.end-animationrange.start+1)
				local c = fmtFCV_Config()
				local t = fmtFCV_Track()
				local x = 1
				local j = 1
				local index = -1
				local tmp = copy boneIdArray #nomap
				local frame = 0
				local tfm = matrix3 1
				local yrp = eulerAngles 0 0 0
				local pos = [0.0, 0.0, 0.0]
				local lastpos = [0.0, 0.0, 0.0]
				local lastrot = [0.0, 0.0, 0.0]
				local key_empty = true
				sort tmp
				
				for i = 1 to tmp.count do (
					x = findItem boneIdArray tmp[i]
					
					o = boneArray[x]
					if doRot and o.controller["Rotation"].keys.count > 0 do (
						
						c = fmtFCV_Config type:2 data:0x60
						t = fmtFCV_Track index:(index+=1)
						
						key_empty = true
						for frame in o.controller["Rotation"].controller.x_rotation.keys do (
							at time frame.time (
								tfm = o.transform
								if o.parent != undefined do (
									tfm *= inverse o.parent.transform
									)
								)
							yrp = tfm.rotationPart as EulerAngles
							if not cullEmpty or (cullEmpty and abs yrp.x > 0.000001) do (
								key_empty = false
								case (bit.shift c.data -4) of (
									0x00: ( -- save as floats, type:0
										append t.x.values #((degToRad yrp.x),0,0,0)
										)
									0x05: ( -- save as ints, type: 80
										append t.x.values #(((degToRad yrp.x) / pi) * 32767.0,0,0,0)
										)
									0x06: ( -- save as ints, type: 96
										append t.x.values #(((degToRad yrp.x) / pi) * 32767.0,0,0,0)
										)
									)
								append t.x.keys (frame.time.frame as integer)
								)
							)
						for frame in o.controller["Rotation"].controller.y_rotation.keys do (
							at time frame.time (
								tfm = o.transform
								if o.parent != undefined do (
									tfm *= inverse o.parent.transform
									)
								)
							yrp = tfm.rotationPart as EulerAngles
							if not cullEmpty or (cullEmpty and abs yrp.y > 0.000001) do (
								key_empty = false
								
								case (bit.shift c.data -4) of (
									0x00: ( -- save as floats, type:0
										append t.y.values #((degToRad yrp.y),0,0,0)
										)
									0x05: ( -- save as ints, type: 80
										append t.y.values #(((degToRad yrp.y) / pi) * 32767.0,0,0,0)
										)
									0x06: ( -- save as ints, type: 96
										append t.y.values #(((degToRad yrp.y) / pi) * 32767.0,0,0,0)
										)
									)
								append t.y.keys (frame.time.frame as integer)
								)
							)
						for frame in o.controller["Rotation"].controller.z_rotation.keys do (
							at time frame.time (
								tfm = o.transform
								if o.parent != undefined do (
									tfm *= inverse o.parent.transform
									)
								)
							yrp = tfm.rotationPart as EulerAngles
							if not cullEmpty or (cullEmpty and abs yrp.z > 0.000001) do (
								key_empty = false
								case (bit.shift c.data -4) of (
									0x00: ( -- save as floats, type:0
										append t.z.values #((degToRad yrp.z),0,0,0)
										)
									0x05: ( -- save as ints, type: 80
										append t.z.values #(((degToRad yrp.z) / pi) * 32767.0,0,0,0)
										)
									0x06: ( -- save as ints, type: 96
										append t.z.values #(((degToRad yrp.z) / pi) * 32767.0,0,0,0)
										)
									)
								append t.z.keys (frame.time.frame as integer)
								)
							)
						
						if not cullEmpty or (cullEmpty and not key_empty) do (
							if doMerge and (j = getNodeIndex tmp[i] 2) > 0 then (
								tracks[j] = t
								config[j] = c
								)
							else (
								if t.x.keys.count > 0 or t.y.keys.count > 0 or t.z.keys.count > 0 do (
									append config c
									append tracks t
									append nodes tmp[i]
									)
								)
							)
						)
					
					if doPos and o.controller["Position"].keys.count > 0 do (
						
						c = (fmtFCV_Config type:4 data:0x15)
						t = fmtFCV_Track index:(index+=1)
						
						key_empty = true
						for frame in o.controller["Position"].controller.x_position.keys do (
							at time frame.time (
								pos = o.position
								if o.parent != undefined do (
									tfm = o.transform * inverse o.parent.transform
									pos = tfm.row4
									)
								)
							if not cullEmpty or (cullEmpty and abs pos.x > 0.000001) do (
								key_empty = false
								case (bit.shift c.data -4) of (
									0x00: ( -- save as floats, type:0
										append t.x.values #((pos.x * mscale),0,0,0)
										)
									0x01: ( -- save as floats, type:21
										append t.x.values #((pos.x * mscale),0,0,0)
										)
									)
								append t.x.keys (frame.time.frame as integer)
								)
							)
						for frame in o.controller["Position"].controller.y_position.keys do (
							at time frame.time (
								pos = o.position
								if o.parent != undefined do (
									tfm = o.transform * inverse o.parent.transform
									pos = tfm.row4
									)
								)
							if not cullEmpty or (cullEmpty and abs pos.y > 0.000001) do (
								key_empty = false
								case (bit.shift c.data -4) of (
									0x00: ( -- save as floats, type:0
										append t.y.values #((pos.y * mscale),0,0,0)
										)
									0x01: ( -- save as floats, type:21
										append t.y.values #((pos.y * mscale),0,0,0)
										)
									)
								append t.y.keys (frame.time.frame as integer)
								)
							)
						for frame in o.controller["Position"].controller.z_position.keys do (
							at time frame.time (
								pos = o.position
								if o.parent != undefined do (
									tfm = o.transform * inverse o.parent.transform
									pos = tfm.row4
									)
								)
							if not cullEmpty or (cullEmpty and abs pos.z > 0.000001) do (
								key_empty = false
								case (bit.shift c.data -4) of (
									0x00: ( -- save as floats, type:0
										append t.z.values #((pos.z * mscale),0,0,0)
										)
									0x01: ( -- save as floats, type:21
										append t.z.values #((pos.z * mscale),0,0,0)
										)
									)
								append t.z.keys (frame.time.frame as integer)
								)
							)
						if not cullEmpty or (cullEmpty and not key_empty) do (
							if doMerge and (j = getNodeIndex tmp[i] 4) > 0 then (
								tracks[j] = t
								config[j] = c
								)
							else (
								if t.x.keys.count > 0 or t.y.keys.count > 0 or t.z.keys.count > 0 do (
									append config c
									append tracks t
									append nodes tmp[i]
									)
								)
							)
						)
					)
				for i = 1 to tracks.count do (tracks[i].index = i) -- set block writting order
				)
			),
		fn getFromScene2 bname_prefix:"bone" = ( -- old function?
			local boneArray = #()
			local boneIdArray = #()
			getBonesFromSelection &boneArray &boneIdArray bname_prefix:bname_prefix
			if boneArray.count > 0 do (
				nodes = #()
				config = #()
				tracks = #()
				num_frames = (animationrange.end-animationrange.start+1)
				local x = 1, i = 1, o
				local index = -1
				local tmp = copy boneIdArray #nomap
				local frame = 0
				local tfm = matrix3 1
				local yrp = eulerAngles 0 0 0
				local pos = [0.0, 0.0, 0.0]
				local t = fmtFCV_Track()
				sort tmp
				
				for i = 1 to tmp.count do (
					x = findItem boneIdArray tmp[i]
					
					o = boneArray[x]
					if o.controller["Rotation"].keys.count > 0 do (
						append config (fmtFCV_Config type:2 data:96)
						
						t = fmtFCV_Track index:(index+=1)
						
						for frame in o.controller["Rotation"].keys do (
							append t.x.keys (frame.time.frame as integer)
							append t.y.keys (frame.time.frame as integer)
							append t.z.keys (frame.time.frame as integer)
							at time frame.time (
								
								tfm = o.transform
								if o.parent != undefined do (
									tfm *= inverse o.parent.transform
									)
								
								yrp = tfm.rotationPart as EulerAngles
								case (bit.shift config[config.count].data -1) of (
									0x00: ( -- save as floats, type:0
										append t.x.values #((degToRad yrp.x),0,0,0)
										append t.y.values #((degToRad yrp.y),0,0,0)
										append t.z.values #((degToRad yrp.z),0,0,0)
										)
									0x05: ( -- save as ints, type: 80
										append t.x.values #(((degToRad yrp.x) / pi) * 32767.0,0,0,0)
										append t.y.values #(((degToRad yrp.y) / pi) * 32767.0,0,0,0)
										append t.z.values #(((degToRad yrp.z) / pi) * 32767.0,0,0,0)
										)
									0x06: ( -- save as ints, type: 96
										append t.x.values #(((degToRad yrp.x) / pi) * 32767.0,0,0,0)
										append t.y.values #(((degToRad yrp.y) / pi) * 32767.0,0,0,0)
										append t.z.values #(((degToRad yrp.z) / pi) * 32767.0,0,0,0)
										)
									)
								)
							)
						append tracks t
						append nodes tmp[i]
						)
					
					if o.controller["Position"].keys.count > 0 do (
						append config (fmtFCV_Config type:4 data:21)
						
						t = fmtFCV_Track index:(index+=1)
						
						for frame in o.controller["Position"].keys do (
							append t.x.keys (frame.time.frame as integer)
							append t.y.keys (frame.time.frame as integer)
							append t.z.keys (frame.time.frame as integer)
							at time frame.time (
								pos = o.position
								
								if o.parent != undefined do (
									tfm = o.transform * inverse o.parent.transform
									pos = tfm.row4
									)
								case (bit.shift config[config.count].data -4) of (
									0x00: ( -- save as floats, type:0
										append t.x.values #((pos.x),0,0,0)
										append t.y.values #((pos.y),0,0,0)
										append t.z.values #((pos.z),0,0,0)
										)
									0x01: ( -- save as ints, type: 80
										append t.x.values #((pos.x),0,0,0)
										append t.y.values #((pos.y),0,0,0)
										append t.z.values #((pos.z),0,0,0)
										)
									)
								)
							)
						append tracks t
						append nodes tmp[i]
						)
					
					
					
					
					)

				
				
				
				)
			
			
			),
		fn sendToScene bname_prefix:"bone" = (
			
			if nodes.count > 0 then (
				local boneArray = #()
				local o = undefined
				local bones_by_name = #()
				local bones_objects = #()
				for o in objects where matchpattern o.name pattern:(bname_prefix + "_*") do (
				--for o in objects where (classof o == boneGeometry or classOf o == Dummy) and matchpattern o.name pattern:"bone_*" do (
					append bones_objects o
					append bones_by_name ((try((subString o.name ((findString o.name "_") + 1) -1) as integer)catch(0)) + 1)
					)
				if bones_by_name.count > 0 do (
					sort bones_by_name
					local i = 1
					local index = 0
					local last_bone = bones_by_name[bones_by_name.count]
					boneArray[last_bone + 1] = undefined
					for i = 1 to bones_objects.count do (
						index = (try((subString bones_objects[i].name ((findString bones_objects[i].name "_") + 1) -1) as integer)catch(0)) + 1
						if index > 0 do boneArray[index] = bones_objects[i]
						)
					)
				/*
				local boneArray = #(
					Bone_000, Bone_001, Bone_002, Bone_003, Bone_004,
					Bone_005, undefined, Bone_007, Bone_008, Bone_009,
					Bone_010, Bone_011, undefined, Bone_013, Bone_014,
					Bone_015, Bone_016, Bone_017, Bone_018, Bone_019,
					Bone_020, undefined, Bone_022, Bone_023, Bone_024,
					undefined, undefined, undefined, undefined, undefined,
					undefined, undefined, Bone_032, Bone_033
					)
				*/
				
				local i = 1
				local x = 1
				local k = 1
				local v = 0.0
				local o
				local t = 1
				local w = false
				local tfm = matrix3 1
				local snodes = #()
				
				-- Collect Only Unique Bone Ids
				for i = 1 to nodes.count do (
					if (x = findItem snodes nodes[i]) == 0 do (
						append snodes nodes[i]
						)
					)
				
				-- Sort Unique List of Bone Ids
				sort snodes
				
				-- Update Timeline Length
				animationRange = interval 0f ((num_frames - 1) as time)
				
				-- Loop through all Unique Bone Ids
				for b in snodes do (
					
					-- Check if Bone Id matches a Bone in the Scene
					if (o = boneArray[b + 1]) == undefined do (
						format "Skipped Bone_%\n" b
						continue
						)
					
					-- Check Each Frame for Updates to Active Bone
					for t = 0 to (num_frames - 1) do (
						
						-- Reset Transform
						tfm = matrix3 1
						
						-- Reset Write State to OFF / False
						w = false
						
						-- Loop Through All nodes, execute only the active bone
						for x = 1 to nodes.count where nodes[x] == b do (
							if (k = findItem tracks[x].x.keys t) > 0 do (
								v = tracks[x].x.values[k][1]
								animate on at time t (
									case config[x].type of (
										0x02: (
											case (bit.shift config[x].data -4) of (
												0x00: (
													if o.parent != undefined then (
														o.rotation.controller.x_rotation = o.rotation.x + (radToDeg v)
														)
													else (
														o.rotation.controller.x_rotation += radToDeg v
														)
													)
												0x01: (
													if o.parent != undefined then (
														o.rotation.controller.x_rotation = o.rotation.x + (radToDeg v)
														)
													else (
														o.rotation.controller.x_rotation = radToDeg v
														)
													)
												0x05: (
													if o.parent != undefined then (
														o.rotation.controller.x_rotation = o.rotation.x + (radToDeg ((v / 32767.0) * pi))
														)
													else (
														o.rotation.controller.x_rotation = radToDeg ((v / 32767.0) * pi)
														)
													)
												0x06: (
													if o.parent != undefined then (
														o.rotation.controller.x_rotation = o.rotation.x + (radToDeg ((v / 32767.0) * pi))
														)
													else (
														o.rotation.controller.x_rotation = radToDeg ((v / 32767.0) * pi)
														)
													)
												)
											)
										0x04: (
											case (bit.shift config[x].data -4) of (
												0x00: (
													if o.parent != undefined then (
														o.pos.controller.x_position = o.parent.pos.x + v
														)
													else (
														o.pos.controller.x_position = v
														)
													)
												0x01: (
													if o.parent != undefined then (
														o.pos.controller.x_position = o.parent.pos.x + v
														)
													else (
														o.pos.controller.x_position = v
														)
													)
												)
											)
										)
									)
								)
							if (k = findItem tracks[x].y.keys (t)) > 0 do (
								v = tracks[x].y.values[k][1]
								animate on at time t (
									case config[x].type of (
										0x02: (
											case (bit.shift config[x].data -4) of (
												0x00: (
													if o.parent != undefined then (
														o.rotation.controller.y_rotation = o.rotation.y + (radToDeg v)
														)
													else (
														o.rotation.controller.y_rotation += radToDeg v
														)
													)
												0x01: (
													if o.parent != undefined then (
														o.rotation.controller.y_rotation = o.rotation.y + (radToDeg v)
														)
													else (
														o.rotation.controller.y_rotation = radToDeg v
														)
													)
												0x05: (
													if o.parent != undefined then (
														o.rotation.controller.y_rotation = o.rotation.y + (radToDeg ((v / 32767.0) * pi))
														)
													else (
														o.rotation.controller.y_rotation = radToDeg ((v / 32767.0) * pi)
														)
													)
												0x06: (
													if o.parent != undefined then (
														o.rotation.controller.y_rotation = o.rotation.y + (radToDeg ((v / 32767.0) * pi))
														)
													else (
														o.rotation.controller.y_rotation = radToDeg ((v / 32767.0) * pi)
														)
													)
												)
											)
										0x04: (
											case (bit.shift config[x].data -4) of (
												0x00: (
													if o.parent != undefined then (
														o.pos.controller.y_position = o.parent.pos.y + v
														)
													else (
														o.pos.controller.y_position = v
														)
													)
												0x01: (
													if o.parent != undefined then (
														o.pos.controller.y_position = o.parent.pos.y + v
														)
													else (
														o.pos.controller.y_position = v
														)
													)
												)
											)
										)
									)
								)
							if (k = findItem tracks[x].z.keys (t)) > 0 do (
								v = tracks[x].z.values[k][1]
								animate on at time t (
									case config[x].type of (
										0x02: (
											case (bit.shift config[x].data -4) of (
												0x00: (
													if o.parent != undefined then (
														o.rotation.controller.z_rotation = o.rotation.z + (radToDeg v)
														
														
														)
													else (
														o.rotation.controller.z_rotation += radToDeg v
														)
													)
												0x01: (
													if o.parent != undefined then (
														o.rotation.controller.z_rotation = o.rotation.z + (radToDeg v)
														)
													else (
														o.rotation.controller.z_rotation = radToDeg v
														)
													)
												0x05: (
													if o.parent != undefined then (
														o.rotation.controller.z_rotation = o.rotation.z + (radToDeg ((v / 32767.0) * pi))
														)
													else (
														o.rotation.controller.z_rotation = radToDeg ((v / 32767.0) * pi)
														)
													)
												0x06: (
													if o.parent != undefined then (
														o.rotation.controller.z_rotation = o.rotation.z + (radToDeg ((v / 32767.0) * pi))
														)
													else (
														o.rotation.controller.z_rotation = radToDeg ((v / 32767.0) * pi)
														)
													)
												)
											)
										0x04: (
											case (bit.shift config[x].data -4) of (
												0x00: (
													if o.parent != undefined then (
														o.pos.controller.z_position = o.parent.pos.z + v
														)
													else (
														o.pos.controller.z_position = v
														)
													)
												0x01: (
													if o.parent != undefined then (
														o.pos.controller.z_position = o.parent.pos.z + v
														)
													else (
														o.pos.controller.z_position = v
														)
													)
												0x01: (
													if o.parent != undefined then (
														o.pos.controller.z_position = o.parent.pos.z + v
														)
													else (
														o.pos.controller.z_position = v
														)
													)
												)
											)
										)
									)
								)
							)
						)
					)
				) else (messageBox "Error:\nFCV Contains No Animations")
			),
		fn sendToScene_with_tangents bname_prefix:"bone" = (
			
			if nodes.count > 0 then (
				local boneArray = #()
				local o = undefined
				local bones_by_name = #()
				local bones_objects = #()
				for o in objects where matchpattern o.name pattern:(bname_prefix + "_*") do (
				--for o in objects where (classof o == boneGeometry or classOf o == Dummy) and matchpattern o.name pattern:"bone_*" do (
					append bones_objects o
					append bones_by_name ((try((subString o.name ((findString o.name "_") + 1) -1) as integer)catch(0)) + 1)
					)
				if bones_by_name.count > 0 do (
					sort bones_by_name
					local i = 1
					local index = 0
					local last_bone = bones_by_name[bones_by_name.count]
					boneArray[last_bone + 1] = undefined
					for i = 1 to bones_objects.count do (
						index = (try((subString bones_objects[i].name ((findString bones_objects[i].name "_") + 1) -1) as integer)catch(0)) + 1
						if index > 0 do boneArray[index] = bones_objects[i]
						)
					)
				/*
				local boneArray = #(
					Bone_000, Bone_001, Bone_002, Bone_003, Bone_004,
					Bone_005, undefined, Bone_007, Bone_008, Bone_009,
					Bone_010, Bone_011, undefined, Bone_013, Bone_014,
					Bone_015, Bone_016, Bone_017, Bone_018, Bone_019,
					Bone_020, undefined, Bone_022, Bone_023, Bone_024,
					undefined, undefined, undefined, undefined, undefined,
					undefined, undefined, Bone_032, Bone_033
					)
				*/
				
				local i = 1
				local x = 1
				local k = 1
				local v = 0.0
				local o
				local t = 1
				local w = false
				local tfm = matrix3 1
				local snodes = #()
				
				-- Collect Only Unique Bone Ids
				for i = 1 to nodes.count do (
					if (x = findItem snodes nodes[i]) == 0 do (
						append snodes nodes[i]
						)
					)
				
				-- Sort Unique List of Bone Ids
				sort snodes
				
				-- Update Timeline Length
				animationRange = interval 0f ((num_frames - 1) as time)
				
				-- Loop through all Unique Bone Ids
				for b in snodes do (
					
					-- Check if Bone Id matches a Bone in the Scene
					if (o = boneArray[b + 1]) == undefined do (
						format "Skipped Bone_%\n" b
						continue
						)
					
					-- Check Each Frame for Updates to Active Bone
					for t = 0 to (num_frames - 1) do (
						
						-- Reset Transform
						tfm = matrix3 1
						
						-- Reset Write State to OFF / False
						w = false
						
						-- Loop Through All nodes, execute only the active bone
						for x = 1 to nodes.count where nodes[x] == b do (
							if (k = findItem tracks[x].x.keys t) > 0 do (
								v = tracks[x].x.values[k][1]
								case config[x].type of (
									0x02: (
										local controller = o.rotation.controller.x_rotation.controller
										addNewKey controller (t as time)
										local newKey = controller.keys[controller.keys.count]
										newKey.inTangentType = #custom
										newKey.outTangentType = #custom
-- 										newKey.outTangentLength = 0.5
-- 										newKey.inTangentLength = 0.5
										case (bit.shift config[x].data -4) of (
											0x00: (
												if o.parent != undefined then (
													newKey.value = o.rotation.x + (radToDeg v)
													)
												else (
													newKey.value = radToDeg v
													)
												newKey.inTangent = tracks[x].x.values[k][2] / 100.0
												newKey.outTangent = tracks[x].x.values[k][3] / -100.0
												)
											0x01: (
												if o.parent != undefined then (
													newKey.value = o.rotation.x + (radToDeg v)
													)
												else (
													newKey.value = radToDeg v
													)
												newKey.inTangent = tracks[x].x.values[k][2] / 32767.0 / 100.0
												newKey.outTangent = tracks[x].x.values[k][3] / -32767.0 / 100.0
												)
											0x05: (
												if o.parent != undefined then (
													newKey.value = o.rotation.x + (radToDeg ((v / 32767.0) * pi))
													)
												else (
													newKey.value = radToDeg ((v / 32767.0) * pi)
													)
												newKey.inTangent = tracks[x].x.values[k][2] / 32767.0 / 100.0
												newKey.outTangent = tracks[x].x.values[k][3] / -32767.0 / 100.0
												)
											0x06: (
												if o.parent != undefined then (
													newKey.value = o.rotation.x + (radToDeg ((v / 32767.0) * pi))
													)
												else (
													newKey.value = radToDeg ((v / 32767.0) * pi)
													)
												newKey.inTangent = tracks[x].x.values[k][2] / 128.0 / 100.0
												newKey.outTangent = tracks[x].x.values[k][3] / -128.0 / 100.0
												)
											)
										)
									0x04: (
										local controller = o.pos.controller.x_position.controller
										addNewKey controller (t as time)
										local newKey = controller.keys[controller.keys.count]
										newKey.inTangentType = #custom
										newKey.outTangentType = #custom
-- 										newKey.outTangentLength = 0.5
-- 										newKey.inTangentLength = 0.5
										case (bit.shift config[x].data -4) of (
											0x00: (
												if o.parent != undefined then (
													newKey.value = o.parent.pos.x + v
													)
												else (
													newKey.value = v
													)
												newKey.inTangent = tracks[x].x.values[k][2] / 100.0
												newKey.outTangent = tracks[x].x.values[k][3] / -100.0
												)
											0x01: (
												if o.parent != undefined then (
													newKey.value = o.parent.pos.x + v
													)
												else (
													newKey.value = v
													)
												newKey.inTangent = tracks[x].x.values[k][2] / 32767.0 / 100.0
												newKey.outTangent = tracks[x].x.values[k][3] / -32767.0 / 100.0
												)
											)
										)
									)
									
								)
							if (k = findItem tracks[x].y.keys (t)) > 0 do (
								v = tracks[x].y.values[k][1]
								case config[x].type of (
									0x02: (
										local controller = o.rotation.controller.y_rotation.controller
										addNewKey controller (t as time)
										local newKey = controller.keys[controller.keys.count]
										newKey.inTangentType = #custom
										newKey.outTangentType = #custom
-- 										newKey.outTangentLength = 0.5
-- 										newKey.inTangentLength = 0.5
										case (bit.shift config[x].data -4) of (
											0x00: (
												if o.parent != undefined then (
													newKey.value = o.rotation.y + (radToDeg v)
													)
												else (
													newKey.value = radToDeg v
													)
												newKey.inTangent = tracks[x].y.values[k][2] / 100.0
												newKey.outTangent = tracks[x].y.values[k][3] / -100.0
												)
											0x01: (
												if o.parent != undefined then (
													newKey.value = o.rotation.y + (radToDeg v)
													)
												else (
													newKey.value = radToDeg v
													)
												newKey.inTangent = tracks[x].y.values[k][2] / 32767.0 / 100.0
												newKey.outTangent = tracks[x].y.values[k][3] / -32767.0 / 100.0
												)
											0x05: (
												if o.parent != undefined then (
													newKey.value = o.rotation.y + (radToDeg ((v / 32767.0) * pi))
													)
												else (
													newKey.value = radToDeg ((v / 32767.0) * pi)
													)
												newKey.inTangent = tracks[x].y.values[k][2] / 32767.0 / 100.0
												newKey.outTangent = tracks[x].y.values[k][3] / -32767.0 / 100.0
												)
											0x06: (
												if o.parent != undefined then (
													newKey.value = o.rotation.y + (radToDeg ((v / 32767.0) * pi))
													)
												else (
													newKey.value = radToDeg ((v / 32767.0) * pi)
													)
												newKey.inTangent = tracks[x].y.values[k][2] / 128.0 / 100.0
												newKey.outTangent = tracks[x].y.values[k][3] / -128.0 / 100.0
												)
											)
										)
									0x04: (
										local controller = o.pos.controller.y_position.controller
										addNewKey controller (t as time)
										local newKey = controller.keys[controller.keys.count]
										newKey.inTangentType = #custom
										newKey.outTangentType = #custom
-- 										newKey.outTangentLength = 0.5
-- 										newKey.inTangentLength = 0.5
										case (bit.shift config[x].data -4) of (
											0x00: (
												if o.parent != undefined then (
													newKey.value = o.parent.pos.y + v
													)
												else (
													newKey.value = v
													)
												newKey.inTangent = tracks[x].y.values[k][2] / 100.0
												newKey.outTangent = tracks[x].y.values[k][3] / -100.0
												)
											0x01: (
												if o.parent != undefined then (
													newKey.value = o.parent.pos.y + v
													)
												else (
													newKey.value = v
													)
												newKey.inTangent = tracks[x].y.values[k][2] / 32767.0 / 100.0
												newKey.outTangent = tracks[x].y.values[k][3] / -32767.0 / 100.0
												)
											)
										)
									)
								)
							if (k = findItem tracks[x].z.keys (t)) > 0 do (
								v = tracks[x].z.values[k][1]
								case config[x].type of (
									0x02: (
										local controller = o.rotation.controller.z_rotation.controller
										addNewKey controller (t as time)
										local newKey = controller.keys[controller.keys.count]
										newKey.inTangentType = #custom
										newKey.outTangentType = #custom
-- 										newKey.outTangentLength = 0.5
-- 										newKey.inTangentLength = 0.5
										case (bit.shift config[x].data -4) of (
											0x00: (
												if o.parent != undefined then (
													newKey.value = o.rotation.z + (radToDeg v)
													)
												else (
													newKey.value = radToDeg v
													)
												newKey.inTangent = tracks[x].z.values[k][2] / 100.0
												newKey.outTangent = tracks[x].z.values[k][3] / -100.0
												)
											0x01: (
												if o.parent != undefined then (
													newKey.value = o.rotation.z + (radToDeg v)
													)
												else (
													newKey.value = radToDeg v
													)
												newKey.inTangent = tracks[x].z.values[k][2] / 32767.0 / 100.0
												newKey.outTangent = tracks[x].z.values[k][3] / -32767.0 / 100.0
												)
											0x05: (
												if o.parent != undefined then (
													newKey.value = o.rotation.z + (radToDeg ((v / 32767.0) * pi))
													)
												else (
													newKey.value = radToDeg ((v / 32767.0) * pi)
													)
												newKey.inTangent = tracks[x].z.values[k][2] / 32767.0 / 100.0
												newKey.outTangent = tracks[x].z.values[k][3] / -32767.0 / 100.0
												)
											0x06: (
												if o.parent != undefined then (
													newKey.value = o.rotation.z + (radToDeg ((v / 32767.0) * pi))
													)
												else (
													newKey.value = radToDeg ((v / 32767.0) * pi)
													)
												newKey.inTangent = tracks[x].z.values[k][2] / 128.0 / 100.0
												newKey.outTangent = tracks[x].z.values[k][3] / -128.0 / 100.0
												)
											)
										)
									0x04: (
										local controller = o.pos.controller.z_position.controller
										addNewKey controller (t as time)
										local newKey = controller.keys[controller.keys.count]
										newKey.inTangentType = #custom
										newKey.outTangentType = #custom
-- 										newKey.outTangentLength = 0.5
-- 										newKey.inTangentLength = 0.5
										case (bit.shift config[x].data -4) of (
											0x00: (
												if o.parent != undefined then (
													newKey.value = o.parent.pos.z + v
													)
												else (
													newKey.value = v
													)
												newKey.inTangent = tracks[x].z.values[k][2] / 10.0
												newKey.outTangent = tracks[x].z.values[k][3] / -10.0
												)
											0x01: (
												if o.parent != undefined then (
													newKey.value = o.parent.pos.z + v
													)
												else (
													newKey.value = v
													)
												newKey.inTangent = tracks[x].z.values[k][2] / 32767.0 / 10.0
												newKey.outTangent = tracks[x].z.values[k][3] / -32767.0 / 10.0
												)
											)
										)
									)
								
								)
							)
						)
					)
				) else (messageBox "Error:\nFCV Contains No Animations")
			),
		fn sendToScene_morph = (
			
			-- get a selection
			local objs = selection as array
			if objs.count > 0 then (
				
				local o = objs[1]
				local j, t
				local morMod = undefined
				local morphIndex = 0
				local frame = 0f
				local val = 100
				local morMax = 0
				
				setCommandPanelTaskMode #modify
				
				if o.modifiers.count > 0 do (
					local m
					for m in o.modifiers where classOf m == Morpher do (
						morMod = m
						exit
						)
					)
				if morMod != undefined then (
					
					-- set as active modifier
					modPanel.setCurrentObject morMod
					
					-- get max weight value
					morMax = morMod.Spinner_Maximum
					
					-- disable limits, the game uses negatives
					morMod.Use_Limits = 0
					
					-- Update Timeline Length
					animationRange = interval 0f ((num_frames - 1) as time)
					
					
					-- For each track in the data
					for t = 1 to tracks.count do (
						
						-- Skip if Not type 4
-- 						if config[t].type != 4 do continue
						
						-- Get the index of the target morpher channel
						morphIndex = nodes[t]
						if morphIndex < 1 do continue
						-- Skip if channel is unused
						
						if (WM3_MC_NumMPts morMod morphIndex) > 0 do (
							print ""
							-- Iterate over keyframe data and create keys at each frame
							for j = 1 to tracks[t].y.num_keys do (
								frame = tracks[t].y.keys[j] -- assuming the keys represent frame numbers
								val = tracks[t].y.values[j][1] as float -- assuming the third value is for Z channel, and scaled to percentage
								format "%: %, %, %\n" morphIndex t frame val
								-- Use WM3_MC_SetValue to set the value at each frame
								with animate on at time frame (
									WM3_MC_SetValue morMod morphIndex val
									)
									
								-- Get the controller for the current morph target
								local controller = morMod[morphIndex].controller
								
								-- Iterate over all keys
								for k = 1 to controller.keys.count do (
									controller.keys[k].inTangentType = #linear
									controller.keys[k].outTangentType = #linear
									)
								
								)
							)
						)
					
					) else (messageBox "Error:\nFailed To Locate Morher")
				
				
				) else (messageBox "Error:\nNothing Selected")
			),
		fn getFromScene_morph = (
			
			-- Get a selection
			local objs = selection as array
			if objs.count > 0 then (
				
				local o = objs[1]
				local morMod = undefined
				local morphIndex = 0
				local frame = 0f
				local val = 0
				local morMax = 0
				local index = -1
				
				setCommandPanelTaskMode #modify
				
				if o.modifiers.count > 0 do (
					local m
					for m in o.modifiers where classOf m == Morpher do (
						morMod = m
						exit
						)
					)
				if morMod != undefined then (
					
					-- Set as active modifier
					modPanel.setCurrentObject morMod
					
					-- Get max weight value
					morMax = morMod.Spinner_Maximum
					
					-- update the frame length
					num_frames = (animationrange.end-animationrange.start).frame as integer
					
					-- Initialize tracks and other related data structures
					tracks = #()
					nodes = #()
					num_tracks = 0
					
					-- Add Dummy
					append nodes 0
					append config (fmtFCV_Config type:4 data:32)
					append tracks (fmtFCV_Track \
						index:num_tracks \
						x:(fmtFCV_Value keys:#(0, num_frames) values:#(#(0.0, 0.0, 0.0, 0.0), #(0.0, 0.0, 0.0, 0.0))) \
						y:(fmtFCV_Value keys:#() values:#()) \
						z:(fmtFCV_Value keys:#() values:#())
						)
					
					-- For each channel in the Morpher
					for morphIndex = 1 to 100 do (
						
						if (WM3_MC_NumMPts morMod morphIndex) > 0 do (
							-- Create a new track
							local track = fmtFCV_Track index:(num_tracks + 1)
							local controller = morMod[morphIndex].controller
							
							-- Read keyframe data from the controller
							for k = 1 to controller.keys.count do (
								frame = controller.keys[k].time
								val = at time frame (WM3_MC_GetValue morMod morphIndex)
								append track.y.keys (frame.frame as integer)
								append track.y.values #(val, 0.0, 0.0, 0.0)
								track.y.num_keys += 1
								)
							
							-- Add the new track to our structures
							if track.y.values.count > 0 do (
								append config (fmtFCV_Config type:4 data:0)
								append nodes morphIndex
								append tracks track
								num_tracks += 1
								)
							)
						)
					) else (messageBox "Error:\nFailed To Locate Morher")
				) else (messageBox "Error:\nNothing Selected")
			),
		fn sendToScene_camera mscale:0.03937007874015748031496 = (
			-- Update Timeline Length
			animationRange = interval 0f ((num_frames - 1) as time)
			
			-- Create the Camera
			local c = Targetcamera fov:45 nearclip:1 farclip:1000 nearrange:0 farrange:1000 mpassEnabled:off mpassRenderPerPass:off transform:(matrix3 [0,-1,0] [0,0,1] [-1,0,0] [0,25,0]) isSelected:on target:(Targetobject transform:(matrix3 [1,0,0] [0,1,0] [0,0,1] [0,-25,0]))
			
			-- For each track in the data
			local t = 1, j = 1, frame = 0, val = 0
			for t = 1 to tracks.count do (
				case nodes[t] of (
					0: ( -- Camera Position
						if config[t].type != 4 and (bit.shift config[t].data -4) != 0 do continue
						
						for j = 1 to tracks[t].x.keys.count do (
							frame = tracks[t].x.keys[j]
							val = tracks[t].x.values[j][1] * mscale
							
							with animate on at time frame (
								c.pos.controller.x_position = val
								)
							)
						for j = 1 to tracks[t].y.keys.count do (
							frame = tracks[t].y.keys[j]
							val = tracks[t].y.values[j][1] * mscale
							
							with animate on at time frame (
								c.pos.controller.z_position = val
								)
							)
						for j = 1 to tracks[t].z.keys.count do (
							frame = tracks[t].z.keys[j]
							val = tracks[t].z.values[j][1] * mscale
							
							with animate on at time frame (
								c.pos.controller.y_position = -val
								)
							)
						)
					
					1: ( -- Camera Target Position
						if config[t].type != 4 and (bit.shift config[t].data -4) != 0 then continue
						
						for j = 1 to tracks[t].x.keys.count do (
							frame = tracks[t].x.keys[j]
							val = tracks[t].x.values[j][1] * mscale
							
							with animate on at time frame (
								c.target.pos.controller.x_position = val
								)
							)
						for j = 1 to tracks[t].y.keys.count do (
							frame = tracks[t].y.keys[j]
							val = tracks[t].y.values[j][1] * mscale
							
							with animate on at time frame (
								c.target.pos.controller.z_position = val
								)
							)
						for j = 1 to tracks[t].z.keys.count do (
							frame = tracks[t].z.keys[j]
							val = tracks[t].z.values[j][1] * mscale
							
							with animate on at time frame (
								c.target.pos.controller.y_position = -val
								)
							)
						)
					
					2: ( -- Camera Roll
						if config[t].type != 2 and (bit.shift config[t].data -4) != 5 then continue
						
						for j = 1 to tracks[t].y.keys.count do (
							frame = tracks[t].y.keys[j]
							val = tracks[t].y.values[j][1] * pi / 32767.0
							
							format "Angle: \t%\n" val
							
							with animate on at time frame (
								c.controller.roll_angle = val
								)
							)
						)
					
					3: ( -- Camera FOV
						if config[t].type != 2 and (bit.shift config[t].data -4) != 10 then continue
						
						for j = 1 to tracks[t].y.keys.count do (
							frame = tracks[t].y.keys[j]
							val = tracks[t].y.values[j][1]
							if val > 0 then val = val / 175.0 else val = 45
							
							with animate on at time frame (
								c.fov = val
								)
							)
						)
					
					)
				)
			)
		)

	local fcv = undefined
	local keys = #()
	local cb_track = undefined -- for save to clip board function
	local sel = 0
	local val = 0
	local txt = ""
	local state = false
	local rc_lbx1 = undefined
	local rc_lbx2 = undefined
	local ue_bone_table = #(
			#( 	-- PL00 Leon
			#("bone_0", "cog"),
			#("bone_1", "spine_01"),
			#("bone_2", "spine_02"),
			#("bone_3", "neck_01"),
			#("bone_4", "head"),
			#("bone_5", "clavicle_r"),
			#("bone_7", "upperarm_r"),
			#("bone_8", "lowerarm_r"),
			#("bone_9", "wrist_r"),
			#("bone_10", "hand_r"),
			#("bone_11", "clavicle_l"),
			#("bone_13", "upperarm_l"),
			#("bone_14", "lowerarm_l"),
			#("bone_15", "wrist_l"),
			#("bone_16", "hand_l"),
			#("bone_17", "pelvis"),
			#("bone_18", "thigh_r"),
			#("bone_19", "calf_r"),
			#("bone_20", "foot_r"),
			#("bone_22", "thigh_l"),
			#("bone_23", "calf_l"),
			#("bone_24", "foot_l"),
			#("bone_32", "ball_r"),
			#("bone_33", "ball_l")
			)
		)
	local anim_types = #(
		0x01,
		0x02,
		0x04,
		0x08,
		0x10,
		0x20,
		0x40,
		0xA0
		)
	local anim_types_list = #(
		"0x01: ??",
		"0x02: R", -- Rotation
		"0x04: M", -- Position
		"0x08: ??",
		"0x10: ??",
		"0x20: ??",
		"0x40: ??",
		"0xA0: ??"
		)
	local data_types = #(
		0x00,
		0x04,
		0x05,
		0x06,
		0x10,
		0x15,
		0x16,
		0x20,
		0x25,
		0x26,
		0x40,
		0x44,
		0x45,
		0x46,
		0x50,
		0x54,
		0x55,
		0x56,
		0x60,
		0x64,
		0x65,
		0x66,
		0x80,
		0x90,
		0x95,
		0x96,
		0xA0,
		0xA4,
		0xA5,
		0xA6,
		0xF0
		)
	local data_types_list = #(
		"0x00: f,f,f",
		"0x04: ",
		"0x05: ",
		"0x06: ",
		"0x10: ",
		"0x15: ",
		"0x16: ",
		"0x20: ",
		"0x25: ",
		"0x26: ",
		"0x40: ",
		"0x44: ",
		"0x45: ",
		"0x46: ",
		"0x50: ",
		"0x54: ",
		"0x55: ",
		"0x56: ",
		"0x60: ",
		"0x64: ",
		"0x65: ",
		"0x66: ",
		"0x80: ",
		"0x90: ",
		"0x95: ",
		"0x96: ",
		"0xA0: ",
		"0xA4: ",
		"0xA5: ",
		"0xA6: ",
		"0xF0: "
		)
	local idlepose = #()
	struct pose_data (
		name = "",
		matrix = (matrix3 1)
		)
	struct clipboard_track (
		config = fmtFCV_Config(),
		nodes = 0,
		addrs = 0,
		tracks = fmtFCV_Track()
		)
	fn addRotationConstraint src tar = (
		if src != undefined and tar != undefined do (
			if classOf src.rotation.controller != Orientation_Constraint do (
				src.rotation.controller = Orientation_Constraint()
				src.rotation.controller.relative = on
				src.rotation.controller.appendTarget tar 0.5
				)
			)
		)
	fn addPositionConstraint src tar = (
		if src != undefined and tar != undefined do (
			if classOf src.pos.controller != Position_Constraint do (
				src.pos.controller = Position_Constraint()
				src.pos.controller.relative = on
				src.pos.controller.appendTarget tar 0.5
				)
			)
		)
	fn skel_const_uhd_to_ue bname_prefix:"bone" = (
		local x = ddl_bone_set.selection
		if x <= ue_bone_table.count and x > 0 do (
			local i = 1
			for i = 1 to ue_bone_table[x].count do (
				addRotationConstraint (getNodeByName ue_bone_table[x][i][1]) (getNodeByName ue_bone_table[x][i][2])
				if matchpattern ue_bone_table[x][i][1] pattern:(bname_prefix + "_0") do (
					addPositionConstraint (getNodeByName ue_bone_table[x][i][1]) (getNodeByName ue_bone_table[x][i][2])
					)
				)
			)
		)
	fn skel_select_const_bones bname_prefix:"bone" = (
		local x = ddl_bone_set.selection
		if x <= ue_bone_table.count and x > 0 do (
			local i = 1
			local o = undefined
			local s = ""
			local j = 0
			local b = #()
			clearSelection()
			for i = 1 to ue_bone_table[x].count do (
				j = findString ue_bone_table[x][i][1] "_"
				if j != undefined and x > 0 do (
					j += 1
					s = bname_prefix + "_" + ((try((subString ue_bone_table[x][i][1] j -1) as integer)catch(ue_bone_table[x][i][1])) as string)
					for o in objects where matchpattern (bname_prefix + "_" + ((try((subString o.name j (findString o.name "_")) as integer)catch(o.name)) as string)) pattern:(s + "*") do (
						append b o
						--format "%\n%\n" s o.name
						exit
						)
					)
				)

			if b.count > 0 do select b
			)
		)
	fn skel_rename_const_bones = (
		local x = ddl_bone_set.selection
		if x <= ue_bone_table.count and x > 0 do (
			local i = 1, o
			clearSelection()
			for i = 1 to ue_bone_table[x].count do (
				if (o = getNodeByName ue_bone_table[x][i][2]) != undefined do o.name = ue_bone_table[x][i][1]
				)
			)
		)
	fn updateBoneName bname_prefix:"bone" = (
		if ddl_bone_set.selection > 0 and ddl_bone_set.selection <= ue_bone_table.count do (
			local b = #("", "")
			local n = ""
			local i = 0
			lbl_bonename.caption = ""
			for b in ue_bone_table[ddl_bone_set.selection] do (
				n = try(b[1])catch("")
				if n != "" and matchpattern n pattern:(bname_prefix + "_*") do (
					i = try((subString n ((findString n "_") + 1) -1) as integer)catch(-1)
					if i > -1 and i == spn2.value do (
						lbl_bonename.caption = try(b[2])catch("")
						exit
						)
					)
				)
			)
		)
	fn updateUI0 = ( -- updates ObjectID, Format, Type
		if lbx1.selection > 0 and lbx1.selection <= fcv.tracks.count do (
			spn2.value = fcv.nodes[lbx1.selection]
			ddl1.selection = findItem data_types fcv.config[lbx1.selection].data
			ddl2.selection = findItem anim_types fcv.config[lbx1.selection].type
			updateBoneName bname_prefix:(edt_bone_prefix.text as string)
			)
		)
	fn updateUI = ( -- Generate Track List, ()
		local TrackList = #()
		local i = 1, ii = 1
		local o = 0
		local c = 0
		local n = "00"
		spn1.value = fcv.num_frames
		if fcv.tracks.count > 0 do (
			TrackList[fcv.tracks.count] = ""
			for i = 1 to fcv.tracks.count do (
				
				o = 0
				c = 0
				if i <= fcv.nodes.count do o = fcv.nodes[i]
				if i <= fcv.config.count do c = fcv.config[i].type
				
				--TrackList[i] = "Track " + (i as string)
				TrackList[i] = (
					(substring (n = "000" + (o as string)) (n.count - 2) 3) + " 0x" + \
					(substring (n = "00" + (bit.IntAsHex c)) (n.count - 1) 2)
					)
				)
			)
		lbx1.items = TrackList
		)
	fn updateUI2 = ( -- Generate Key list
		local KeyList = #()
		local i = lbx1.selection
		local ii = 1
		local index = 0
		keys = #()
		if i > 0 do (
			for ii = 1 to fcv.tracks[i].x.keys.count do (if (index = findItem keys fcv.tracks[i].x.keys[ii]) == 0 do (append keys fcv.tracks[i].x.keys[ii]))
			for ii = 1 to fcv.tracks[i].y.keys.count do (if (index = findItem keys fcv.tracks[i].y.keys[ii]) == 0 do (append keys fcv.tracks[i].y.keys[ii]))
			for ii = 1 to fcv.tracks[i].z.keys.count do (if (index = findItem keys fcv.tracks[i].z.keys[ii]) == 0 do (append keys fcv.tracks[i].z.keys[ii]))
			if keys.count > 0 do (
				sort keys
				KeyList[keys.count] = ""
				for ii = 1 to keys.count do (
					KeyList[ii] = "Frame " + (keys[ii] as string)
					)
				)
			)
		lbx2.items = KeyList
		)
	fn updateUI3 = ( -- Generate Value List
		local ValueList = #()
		local t = lbx1.selection
		local k = lbx2.selection
		
		if t > 0 and t <= fcv.tracks.count then (
			if k > 0 and k <= keys.count then (
				local i = 1
				
				ValueList = #("X:", "Z:", "Y:")
				spn3.value = keys[k]
				
				for i = 1 to fcv.tracks[t].x.keys.count do (
					if fcv.tracks[t].x.keys[i] == keys[k] do (
						ValueList[1] = ("X: " + (fcv.tracks[t].x.values[i] as string))
						exit
						)
					)
				for i = 1 to fcv.tracks[t].y.keys.count do (
					if fcv.tracks[t].y.keys[i] == keys[k] do (
						ValueList[2] = ("Z: " + (fcv.tracks[t].y.values[i] as string))
						exit
						)
					)
				for i = 1 to fcv.tracks[t].z.keys.count do (
					if fcv.tracks[t].z.keys[i] == keys[k] do (
						ValueList[3] = ("Y: " + (fcv.tracks[t].z.values[i] as string))
						exit
						)
					)
				)
			else (
				lbx2.selection = 0
				if keys.count > 0 do (
					lbx2.selection = 1
					updateUI2()
					)
				)
			)
		else (
			lbx1.selection = 0
			if fcv.tracks.count > 0 do (
				lbx1.selection = 1
				updateUI()
				)
			)
		
		
		lbx3.items = ValueList
		
		)
	fn updateUI3_Value = ( -- Change Frame Number
		local t = lbx1.selection
		local k = lbx2.selection
		if t > 0 and t <= fcv.tracks.count then (
			if k > 0 and k <= keys.count then (
				local i = 1
				
				
				for i = 1 to fcv.tracks[t].x.keys.count do (
					if fcv.tracks[t].x.keys[i] == keys[k] do (
						fcv.tracks[t].x.keys[i] = spn3.value
						exit
						)
					)
				for i = 1 to fcv.tracks[t].y.keys.count do (
					if fcv.tracks[t].y.keys[i] == keys[k] do (
						fcv.tracks[t].y.keys[i] = spn3.value
						exit
						)
					)
				for i = 1 to fcv.tracks[t].z.keys.count do (
					if fcv.tracks[t].z.keys[i] == keys[k] do (
						fcv.tracks[t].z.keys[i] = spn3.value
						exit
						)
					)
				lbx2.items[k] = "Frame " + ((keys[k] = spn3.value) as string)
				)
			else (
				lbx2.selection = 0
				if keys.count > 0 do (
					lbx2.selection = 1
					updateUI2()
					)
				)
			)
		else (
			lbx1.selection = 0
			if fcv.tracks.count > 0 do (
				lbx1.selection = 1
				updateUI()
				)
			)
		
		
		)
	fn updateUI4 = ( -- On Value List Selection, Update EditText Boxes
		local t = lbx1.selection -- track index
		local k = lbx2.selection -- key index
		local v = lbx3.selection -- value index (x,y,z)
		local i = 1
		local isEnabled = false
		--format "%/%\t%/%\t%/%\n" t fcv.tracks.count k keys.count v 3
		if t > 0 and k > 0 and v > 0 and t <= fcv.tracks.count and k <= keys.count and v <= 3 do (
			
			edt1.text = edt2.text = edt3.text = edt4.text = "0.0"
			edt1.enabled = edt2.enabled = edt3.enabled = edt4.enabled = chk1.checked = false
			case v of (
				
				1: (	-- X
					
					for i = 1 to fcv.tracks[t].x.keys.count do (
						if fcv.tracks[t].x.keys[i] == keys[k] do (
							edt1.text = fcv.tracks[t].x.values[i][1] as string
							edt2.text = fcv.tracks[t].x.values[i][2] as string
							edt3.text = fcv.tracks[t].x.values[i][3] as string
							edt4.text = fcv.tracks[t].x.values[i][4] as string
							isEnabled = true
							exit
							)
						)
					
					)
				2: (	-- Y
					
					for i = 1 to fcv.tracks[t].y.keys.count do (
						if fcv.tracks[t].y.keys[i] == keys[k] do (
							edt1.text = fcv.tracks[t].y.values[i][1] as string
							edt2.text = fcv.tracks[t].y.values[i][2] as string
							edt3.text = fcv.tracks[t].y.values[i][3] as string
							edt4.text = fcv.tracks[t].y.values[i][4] as string
							isEnabled = true
							exit
							)
						)
					
					)
				3: (	-- Z
					
					for i = 1 to fcv.tracks[t].z.keys.count do (
						if fcv.tracks[t].z.keys[i] == keys[k] do (
							edt1.text = fcv.tracks[t].z.values[i][1] as string
							edt2.text = fcv.tracks[t].z.values[i][2] as string
							edt3.text = fcv.tracks[t].z.values[i][3] as string
							edt4.text = fcv.tracks[t].z.values[i][4] as string
							isEnabled = true
							exit
							)
						)
					
					)
				)
			
			
			edt1.enabled = edt2.enabled = edt3.enabled = edt4.enabled = chk1.checked = isEnabled
			)
		)
	fn updateUI4_Value = ( -- On EditText Box Updated
		local t = lbx1.selection -- track index
		local k = lbx2.selection -- key index
		local v = lbx3.selection -- value index (x,y,z)
		local i = 1
		
		--format "%/%\t%/%\t%/%\n" t fcv.tracks.count k keys.count v 3
		if t > 0 and k > 0 and v > 0 and t <= fcv.tracks.count and k <= keys.count and v <= 3 do (
			case v of (
				1: (	-- X
					for i = 1 to fcv.tracks[t].x.keys.count do (
						if fcv.tracks[t].x.keys[i] == keys[k] do (
							fcv.tracks[t].x.values[i][1] = try(execute edt1.text)catch(fcv.tracks[t].x.values[i][1])
							fcv.tracks[t].x.values[i][2] = try(execute edt2.text)catch(fcv.tracks[t].x.values[i][2])
							fcv.tracks[t].x.values[i][3] = try(execute edt3.text)catch(fcv.tracks[t].x.values[i][3])
							fcv.tracks[t].x.values[i][4] = try(execute edt4.text)catch(fcv.tracks[t].x.values[i][4])
							exit
							)
						)
					)
				2: (	-- X
					for i = 1 to fcv.tracks[t].y.keys.count do (
						if fcv.tracks[t].y.keys[i] == keys[k] do (
							fcv.tracks[t].y.values[i][1] = try(execute edt1.text)catch(fcv.tracks[t].y.values[i][1])
							fcv.tracks[t].y.values[i][2] = try(execute edt2.text)catch(fcv.tracks[t].y.values[i][2])
							fcv.tracks[t].y.values[i][3] = try(execute edt3.text)catch(fcv.tracks[t].y.values[i][3])
							fcv.tracks[t].y.values[i][4] = try(execute edt4.text)catch(fcv.tracks[t].y.values[i][4])
							exit
							)
						)
					)
				3: (	-- X
					for i = 1 to fcv.tracks[t].z.keys.count do (
						if fcv.tracks[t].z.keys[i] == keys[k] do (
							fcv.tracks[t].z.values[i][1] = try(execute edt1.text)catch(fcv.tracks[t].z.values[i][1])
							fcv.tracks[t].z.values[i][2] = try(execute edt2.text)catch(fcv.tracks[t].z.values[i][2])
							fcv.tracks[t].z.values[i][3] = try(execute edt3.text)catch(fcv.tracks[t].z.values[i][3])
							fcv.tracks[t].z.values[i][4] = try(execute edt4.text)catch(fcv.tracks[t].z.values[i][4])
							exit
							)
						)
					)
				)
			)
		updateUI3()
		)
	fn sort_keys &arr new_key_int = (
		-- add the entry
		arr.num_keys += 1
		append arr.keys new_key_int
		append arr.values #(0.01, 0.02, 0.03, 0.04)
		
		-- sort the entry
		local i = 1
		local index = 0
		local keys_t = copy arr.keys #nomap
		local values_t = copy arr.values #nomap
		sort arr.keys
		for i = 1 to keys_t.count do (
			index = findItem arr.keys keys_t[i]
			arr.values[index] = values_t[i]
			)
		)
	fn updateUI4_Enable state = ( -- On EditText Box Toggled
		local key_pos = 0
		local t = lbx1.selection
		local k = lbx2.selection
		local v = lbx3.selection
		if t > 0 and t <= fcv.tracks.count and k > 0 and k <= keys.count and v > 0 and v <= 3 do (
			case v of (
				1: (
					if (key_pos = findItem fcv.tracks[t].x.keys keys[k]) == 0 and state then (
						sort_keys &fcv.tracks[t].x keys[k]
						)
					else (
						fcv.tracks[t].x.num_keys -= 1
						deleteItem fcv.tracks[t].x.keys key_pos
						deleteItem fcv.tracks[t].x.values key_pos
						)
					)
				2: (
					if (key_pos = findItem fcv.tracks[t].y.keys keys[k]) == 0 and state then (
						sort_keys &fcv.tracks[t].y keys[k]
						)
					else (
						fcv.tracks[t].y.num_keys -= 1
						deleteItem fcv.tracks[t].y.keys key_pos
						deleteItem fcv.tracks[t].y.values key_pos
						)
					)
				3: (
					if (key_pos = findItem fcv.tracks[t].z.keys keys[k]) == 0 and state then (
						sort_keys &fcv.tracks[t].z keys[k]
						)
					else (
						fcv.tracks[t].z.num_keys -= 1
						deleteItem fcv.tracks[t].z.keys key_pos
						deleteItem fcv.tracks[t].z.values key_pos
						)
					)
				)
			)
		updateUI3()
		updateUI4()
		)
	fn add_track = (
		local t = lbx1.selection
		if t > 0 and t <= fcv.tracks.count then (
			t += 1
			insertItem (fmtFCV_Track()) fcv.tracks t
			insertItem (fmtFCV_Config()) fcv.config t
			insertItem 0 fcv.addrs t
			insertItem 0 fcv.nodes t
			)
		else (
			append fcv.tracks (fmtFCV_Track())
			append fcv.config (fmtFCV_Config())
			append fcv.addrs 0
			append fcv.nodes 0
			)
		updateUI()
		updateUI0()
		updateUI2()
		updateUI3()
		updateUI4()
		)
	fn add_key = (
		local t = lbx1.selection
		if fcv != undefined and t > 0 and t <= fcv.tracks.count do (
			fcv.tracks[t].x.num_keys += 1
			fcv.tracks[t].y.num_keys += 1
			fcv.tracks[t].z.num_keys += 1
			append fcv.tracks[t].x.keys fcv.tracks[t].x.num_keys
			append fcv.tracks[t].y.keys fcv.tracks[t].y.num_keys
			append fcv.tracks[t].z.keys fcv.tracks[t].z.num_keys
			append fcv.tracks[t].x.values (#(0,0,0,0))
			append fcv.tracks[t].y.values (#(0,0,0,0))
			append fcv.tracks[t].z.values (#(0,0,0,0))
			updateUI2()
			lbx2.selection = fcv.tracks.count
			updateUI3()
			)
		)
	fn erase_track = (
		local t = lbx1.selection
		if t > 0 and t <= fcv.tracks.count do (
			deleteItem fcv.tracks t
			deleteItem fcv.nodes t
			deleteItem fcv.config t
			deleteItem fcv.addrs t
			updateUI()
			updateUI0()
			updateUI2()
			updateUI3()
			updateUI4()
			)
		)
	fn erase_key = (
		local t = lbx1.selection
		local k = lbx2.selection
		local key_pos = 0
		if t > 0 and t <= fcv.tracks.count and k > 0 and k <= keys.count do (
			if (key_pos = findItem fcv.tracks[t].x.keys keys[k]) > 0 then (
				fcv.tracks[t].x.num_keys -= 1
				deleteItem fcv.tracks[t].x.keys key_pos
				deleteItem fcv.tracks[t].x.values key_pos
				)
			if (key_pos = findItem fcv.tracks[t].y.keys keys[k]) > 0 then (
				fcv.tracks[t].y.num_keys -= 1
				deleteItem fcv.tracks[t].y.keys key_pos
				deleteItem fcv.tracks[t].y.values key_pos
				)
			if (key_pos = findItem fcv.tracks[t].z.keys keys[k]) > 0 then (
				fcv.tracks[t].z.num_keys -= 1
				deleteItem fcv.tracks[t].z.keys key_pos
				deleteItem fcv.tracks[t].z.values key_pos
				)
			deleteItem keys k
			)
		updateUI2()
		)
	fn read = (
		local file = GetOpenFileName types:"FCV files (*.fcv)|*.fcv|All files (*.*)|*.*|"
		if file != undefined do (
			fcv = fmtFCV()
			fcv.read(file)
			lbl_file.caption = try(filenameFromPath file)catch(lbl_file.caption)
			)
		)
	fn write = (
		local file = GetSaveFileName types:"FCV files (*.fcv)|*.fcv|All files (*.*)|*.*|"
		if file != undefined do (
			fcv.write(file)
			lbl_file.caption = try(filenameFromPath file)catch(lbl_file.caption)
			)
		)
	fn import_bin_skeleton file bname_prefix:"bone" = (
		if file != undefined and file != "" do (
			local f = try(fopen file "rb")catch(undefined)
			if f != undefined then (
				local bone_addr = readLong f #unsigned
				if bone_addr == 0x60 then (
					fseek f 0x19 #seek_set
					local bone_count = readByte f #unsigned
					if bone_count > 0 then (
						delete $*
						local bone_name = bname_prefix + "_0"
						local col_left = color 6 134 6
						local col_left_alt = color 113 134 6
						local col_right = color 28 28 177
						local col_right_alt = color 108 8 136
						local col_mid = color 8 110 134
						local col_root = color 224 198 87
						local col_head = color 166 202 240
						local bin_bones = #()
						local root_bone = Dummy boxSize:[1000, 1, 1000] position:[0,0,0] name:"ROOT"
						bin_bones[bone_count] = undefined
						local i = 1
						local parArray = #()
						local idArray = #()
						idArray[bone_count] = 0
						parArray[bone_count] = undefined
						fseek f bone_addr #seek_set
						for i = 1 to bone_count do (
							bone_name = bname_prefix + "_" + ((idArray[i] = readByte f #unsigned) as string)
							parArray[i] = readByte f #signed + 1
							fseek f 2 #seek_cur
							bin_bones[i] = Dummy boxSize:[100, 100, 100] showLinksOnly:true showLinks:true name:bone_name position:[readFloat f, readFloat f, readFloat f]
							)
						for i = 1 to bone_count do (
							if parArray[i] > 0 then (
								bin_bones[i].position += bin_bones[parArray[i]].position
								bin_bones[i].parent = bin_bones[parArray[i]]
								)
							else (
								bin_bones[i].parent = root_bone
								)
							)
						
						for i = 1 to bone_count do (
							if bin_bones[i].position.x < 0.0 then (
								if bin_bones[i].children.count > 0 or idArray[i] < 32 then (
									bin_bones[i].wirecolor = col_left
									)
								else (
									bin_bones[i].wirecolor = col_left_alt
									)
								)
							else (
								if bin_bones[i].children.count > 0 or idArray[i] < 32 then (
									bin_bones[i].wirecolor = col_right
									)
								else (
									bin_bones[i].wirecolor = col_right_alt
									)
								)
							if ((bin_bones[i].position.x * 1000.0) as integer) == 0 do (
								bin_bones[i].wirecolor = col_mid
								)
							if idArray[i] == 4 do (
								bin_bones[i].wirecolor = col_head
								)
							if bin_bones[i].parent == root_bone do (
								bin_bones[i].wirecolor = col_root
								bin_bones[i].showLinksOnly = false
								bin_bones[i].showLinks = false
								)
							)
						root_bone.transform = (matrix3 [0.0393701,0,0] [0,0,0.0393701] [0,-0.0393701,0] [0,0,0])
						idlepose = #()
						idlepose[bone_count] = pose_data()
						for i = 1 to bone_count do (
							idlepose[i] = (pose_data name:bin_bones[i].name matrix:bin_bones[i].transform)
							)
						append idlepose (pose_data name:root_bone.name matrix:root_bone.transform)
						) else (messageBox "error:\nbin mesh contains no bones")
					) else (messageBox "error:\nunexpected bone address, file is ignored")
				fclose f
				) else (format "failed to open file: %\n" file)
			)
		)
	fn build_pl00_skeleton = (
		-- Build Leon's PL00 Skeleton
		local d = [0, 0, 1]
		with redraw off undo off (
			local Bone_Root = Dummy name:"ROOT"  boxSize:[500,500,500] position:[0,0,0] wirecolor:(color 115 115 115) -- ROOT
			local Bone_000 = Dummy name:"Center Of Gravity"  boxSize:[100,100,100] parent:Bone_Root position:[0,1140,0] showLinksOnly:true showLinks:true wirecolor:(color 115 115 115) -- COG
			local Bone_001 = Dummy name:"Spine 1"  boxSize:[100,100,100] parent:Bone_000 position:[0,1150,-0.0133073] showLinksOnly:true showLinks:true wirecolor:(color 8 110 134) -- spine01
			local Bone_002 = Dummy name:"Spine 2"  boxSize:[100,100,100] parent:Bone_001 position:[0,1360,-13.4001] showLinksOnly:true showLinks:true wirecolor:(color 8 110 134) -- spine02
			local Bone_003 = Dummy name:"Neck"  boxSize:[100,100,100] parent:Bone_002 position:[-2.07424e-05,1560,-34.4001] showLinksOnly:true showLinks:true wirecolor:(color 8 110 134) -- neck01
			local Bone_004 = Dummy name:"Head"  boxSize:[100,100,100] parent:Bone_003 position:[-4.33424e-05,1650,-23.4] showLinksOnly:true showLinks:true wirecolor:(color 166 202 240) -- head
			local Bone_005 = Dummy name:"Clavicle R"  boxSize:[100,100,100] parent:Bone_002 position:[-45,1465,-34.4001] showLinksOnly:true showLinks:true wirecolor:(color 6 134 6) -- clavicle_r
			local Bone_007 = Dummy name:"Upperarm R"  boxSize:[100,100,100] parent:Bone_005 position:[-172,1507.83,-34.4001] showLinksOnly:true showLinks:true wirecolor:(color 6 134 6) -- upperarm_r
			local Bone_008 = Dummy name:"Lowerarm R"  boxSize:[100,100,100] parent:Bone_007 position:[-436,1507.83,-34.4001] showLinksOnly:true showLinks:true wirecolor:(color 6 134 6) -- lowerarm_r
			local Bone_009 = Dummy name:"Wrist R"  boxSize:[100,100,100] parent:Bone_008 position:[-590,1507.83,-34.4001] showLinksOnly:true showLinks:true wirecolor:(color 6 134 6) -- wrist_r
			local Bone_010 = Dummy name:"Hand R"  boxSize:[100,100,100] parent:Bone_009 position:[-704,1507.83,-34.4002] showLinksOnly:true showLinks:true wirecolor:(color 6 134 6) -- hand_r
			local Bone_011 = Dummy name:"Clavicle L"  boxSize:[100,100,100] parent:Bone_002 position:[45,1465,-34.4001] showLinksOnly:true showLinks:true wirecolor:(color 28 28 177) -- clavicle_l
			local Bone_013 = Dummy name:"Upperarm L"  boxSize:[100,100,100] parent:Bone_011 position:[172,1507.83,-34.4001] showLinksOnly:true showLinks:true wirecolor:(color 28 28 177) -- upperarm_l
			local Bone_014 = Dummy name:"Lowerarm L"  boxSize:[100,100,100] parent:Bone_013 position:[436,1507.83,-34.4001] showLinksOnly:true showLinks:true wirecolor:(color 28 28 177) -- lowerarm_l
			local Bone_015 = Dummy name:"Wrist L"  boxSize:[100,100,100] parent:Bone_014 position:[590,1507.83,-34.4001] showLinksOnly:true showLinks:true wirecolor:(color 28 28 177) -- wrist_l
			local Bone_016 = Dummy name:"Hand L"  boxSize:[100,100,100] parent:Bone_015 position:[704,1507.83,-34.4002] showLinksOnly:true showLinks:true wirecolor:(color 28 28 177) -- hand_l
			local Bone_017 = Dummy name:"Pelvis"  boxSize:[100,100,100] parent:Bone_000 position:[0,1140,0] showLinksOnly:true showLinks:true wirecolor:(color 224 198 87) -- pelvis
			local Bone_018 = Dummy name:"Thigh R"  boxSize:[100,100,100] parent:Bone_017 position:[-92,1010,2.11797e-05] showLinksOnly:true showLinks:true wirecolor:(color 6 134 6) -- thigh_r
			local Bone_019 = Dummy name:"Calf R"  boxSize:[100,100,100] parent:Bone_018 position:[-92,574.51,3.10948e-05] showLinksOnly:true showLinks:true wirecolor:(color 6 134 6) -- calf_r
			local Bone_020 = Dummy name:"Foot R"  boxSize:[100,100,100] parent:Bone_019 position:[-92,135,-4.11142e-06] showLinksOnly:true showLinks:true wirecolor:(color 6 134 6) -- foot_r
			local Bone_022 = Dummy name:"Thigh L"  boxSize:[100,100,100] parent:Bone_017 position:[92,1010,2.11797e-05] showLinksOnly:true showLinks:true wirecolor:(color 28 28 177) -- thigh_l
			local Bone_023 = Dummy name:"Calf L"  boxSize:[100,100,100] parent:Bone_022 position:[92,574.51,3.10949e-05] showLinksOnly:true showLinks:true wirecolor:(color 28 28 177) -- calf_l
			local Bone_024 = Dummy name:"Foot L"  boxSize:[100,100,100] parent:Bone_023 position:[92,135,-1.93702e-05] showLinksOnly:true showLinks:true wirecolor:(color 28 28 177) -- foot_l
			local Bone_032 = Dummy name:"Eyeball L"  boxSize:[100,100,100] parent:Bone_004 position:[-27.88,1725.75,61.59] showLinksOnly:true showLinks:true wirecolor:(color 224 198 87) -- ball_r
			local Bone_033 = Dummy name:"Eyeball R"  boxSize:[100,100,100] parent:Bone_004 position:[27.8825,1725.75,61.5943] showLinksOnly:true showLinks:true wirecolor:(color 134 6 6) -- ball_l
-- 			
-- 			local Bone_000 = Dummy name:"Center Of Gravity"  boxSize:[100,100,100] parent:Bone_Root transform:(matrix3 [1,0,0] [0,1,0] [0,0,1] [-0.000133412,7.17046e-07,11.7393]) showLinksOnly:true showLinks:true wirecolor:(color 115 115 115) -- COG
-- 			local Bone_001 = Dummy name:"Spine 1"  boxSize:[100,100,100] parent:Bone_000 transform:(matrix3 [1,0,0] [0,1,0] [0,0,1] [-0.000133412,0.000133717,11.8393]) showLinksOnly:true showLinks:true wirecolor:(color 8 110 134) -- spine01
-- 			local Bone_002 = Dummy name:"Spine 2"  boxSize:[100,100,100] parent:Bone_001 transform:(matrix3 [1,0,0] [0,1,0] [0,0,1] [-0.000133412,0.134001,13.9393]) showLinksOnly:true showLinks:true wirecolor:(color 8 110 134) -- spine02
-- 			local Bone_003 = Dummy name:"Neck"  boxSize:[100,100,100] parent:Bone_002 transform:(matrix3 [1,0,0] [0,1,0] [0,0,1] [-0.000133412,0.344001,15.9393]) showLinksOnly:true showLinks:true wirecolor:(color 8 110 134) -- neck01
-- 			local Bone_004 = Dummy name:"Head"  boxSize:[100,100,100] parent:Bone_003 transform:(matrix3 [1,0,0] [0,1,0] [0,0,1] [-0.000133412,0.234001,16.8393]) showLinksOnly:true showLinks:true wirecolor:(color 166 202 240) -- head
-- 			local Bone_005 = Dummy name:"Clavicle R"  boxSize:[100,100,100] parent:Bone_002 transform:(matrix3 [1,0,0] [0,1,0] [0,0,1] [-0.450133,0.344001,14.9893]) showLinksOnly:true showLinks:true wirecolor:(color 6 134 6) -- clavicle_r
-- 			local Bone_007 = Dummy name:"Upperarm R"  boxSize:[100,100,100] parent:Bone_005 transform:(matrix3 [1,0,0] [0,1,0] [0,0,1] [-1.72013,0.344001,15.4176]) showLinksOnly:true showLinks:true wirecolor:(color 6 134 6) -- upperarm_r
-- 			local Bone_008 = Dummy name:"Lowerarm R"  boxSize:[100,100,100] parent:Bone_007 transform:(matrix3 [1,0,0] [0,1,0] [0,0,1] [-4.36013,0.344001,15.4176]) showLinksOnly:true showLinks:true wirecolor:(color 6 134 6) -- lowerarm_r
-- 			local Bone_009 = Dummy name:"Wrist R"  boxSize:[100,100,100] parent:Bone_008 transform:(matrix3 [1,0,0] [0,1,0] [0,0,1] [-5.90013,0.344001,15.4176]) showLinksOnly:true showLinks:true wirecolor:(color 6 134 6) -- wrist_r
-- 			local Bone_010 = Dummy name:"Hand R"  boxSize:[100,100,100] parent:Bone_009 transform:(matrix3 [1,0,0] [0,1,0] [0,0,1] [-7.04013,0.344001,15.4176]) showLinksOnly:true showLinks:true wirecolor:(color 6 134 6) -- hand_r
-- 			local Bone_011 = Dummy name:"Clavicle L"  boxSize:[100,100,100] parent:Bone_002 transform:(matrix3 [1,0,0] [0,1,0] [0,0,1] [0.449867,0.344001,14.9893]) showLinksOnly:true showLinks:true wirecolor:(color 28 28 177) -- clavicle_l
-- 			local Bone_013 = Dummy name:"Upperarm L"  boxSize:[100,100,100] parent:Bone_011 transform:(matrix3 [1,0,0] [0,1,0] [0,0,1] [1.71987,0.344001,15.4176]) showLinksOnly:true showLinks:true wirecolor:(color 28 28 177) -- upperarm_l
-- 			local Bone_014 = Dummy name:"Lowerarm L"  boxSize:[100,100,100] parent:Bone_013 transform:(matrix3 [1,0,0] [0,1,0] [0,0,1] [4.35987,0.344001,15.4176]) showLinksOnly:true showLinks:true wirecolor:(color 28 28 177) -- lowerarm_l
-- 			local Bone_015 = Dummy name:"Wrist L"  boxSize:[100,100,100] parent:Bone_014 transform:(matrix3 [1,0,0] [0,1,0] [0,0,1] [5.89987,0.344001,15.4176]) showLinksOnly:true showLinks:true wirecolor:(color 28 28 177) -- wrist_l
-- 			local Bone_016 = Dummy name:"Hand L"  boxSize:[100,100,100] parent:Bone_015 transform:(matrix3 [1,0,0] [0,1,0] [0,0,1] [7.03987,0.344001,15.4176]) showLinksOnly:true showLinks:true wirecolor:(color 28 28 177) -- hand_l
-- 			local Bone_017 = Dummy name:"Pelvis"  boxSize:[100,100,100] parent:Bone_000 transform:(matrix3 [1,0,0] [0,1,0] [0,0,1] [-0.000133412,7.17046e-07,11.7393]) showLinksOnly:true showLinks:true wirecolor:(color 224 198 87) -- pelvis
-- 			local Bone_018 = Dummy name:"Thigh R"  boxSize:[100,100,100] parent:Bone_017 transform:(matrix3 [1,0,0] [0,1,0] [0,0,1] [-0.920133,7.17046e-07,10.4393]) showLinksOnly:true showLinks:true wirecolor:(color 6 134 6) -- thigh_r
-- 			local Bone_019 = Dummy name:"Calf R"  boxSize:[100,100,100] parent:Bone_018 transform:(matrix3 [1,0,0] [0,1,0] [0,0,1] [-0.920133,7.17046e-07,6.08436]) showLinksOnly:true showLinks:true wirecolor:(color 6 134 6) -- calf_r
-- 			local Bone_020 = Dummy name:"Foot R"  boxSize:[100,100,100] parent:Bone_019 transform:(matrix3 [1,0,0] [0,1,0] [0,0,1] [-0.920133,7.17046e-07,1.68926]) showLinksOnly:true showLinks:true wirecolor:(color 6 134 6) -- foot_r
-- 			local Bone_022 = Dummy name:"Thigh L"  boxSize:[100,100,100] parent:Bone_017 transform:(matrix3 [1,0,0] [0,1,0] [0,0,1] [0.919867,7.17046e-07,10.4393]) showLinksOnly:true showLinks:true wirecolor:(color 28 28 177) -- thigh_l
-- 			local Bone_023 = Dummy name:"Calf L"  boxSize:[100,100,100] parent:Bone_022 transform:(matrix3 [1,0,0] [0,1,0] [0,0,1] [0.919867,7.17046e-07,6.08436]) showLinksOnly:true showLinks:true wirecolor:(color 28 28 177) -- calf_l
-- 			local Bone_024 = Dummy name:"Foot L"  boxSize:[100,100,100] parent:Bone_023 transform:(matrix3 [1,0,0] [0,1,0] [0,0,1] [0.919867,1.71705e-06,1.68926]) showLinksOnly:true showLinks:true wirecolor:(color 28 28 177) -- foot_l
-- 			local Bone_032 = Dummy name:"Eyeball L"  boxSize:[100,100,100] parent:Bone_004 transform:(matrix3 [1,0,0] [0,1,0] [0,0,1] [-0.278933,-0.615899,17.5968]) showLinksOnly:true showLinks:true wirecolor:(color 224 198 87) -- ball_r
-- 			local Bone_033 = Dummy name:"Eyeball R"  boxSize:[100,100,100] parent:Bone_004 transform:(matrix3 [1,0,0] [0,1,0] [0,0,1] [0.278693,-0.61594,17.5968]) showLinksOnly:true showLinks:true wirecolor:(color 134 6 6) -- ball_l
-- 			


-- 			local Bone_000 = BoneSys.createBone (b = [0,1140,0]) (b + [0,1,0]) d; Bone_000.parent = Bone_Root; Bone_000.showLinksOnly = true; Bone_000.showLinks = true; Bone_000.wirecolor = (color 115 115 115); Bone_000.boneEnable=false  -- COG
-- 			local Bone_001 = BoneSys.createBone (b = [0,1150,-0.0133073]) (b + [0,1,0]) d; Bone_001.parent = Bone_000; Bone_001.showLinksOnly = true; Bone_001.showLinks = true; Bone_001.wirecolor = (color 8 110 134); Bone_001.boneEnable=false  -- spine01
-- 			local Bone_002 = BoneSys.createBone (b = [0,1360,-13.4001]) (b + [0,1,0]) d; Bone_002.parent = Bone_001; Bone_002.showLinksOnly = true; Bone_002.showLinks = true; Bone_002.wirecolor = (color 8 110 134); Bone_002.boneEnable=false  -- spine02
-- 			local Bone_003 = BoneSys.createBone (b = [-2.07424e-05,1560,-34.4001]) (b + [0,1,0]) d; Bone_003.parent = Bone_002; Bone_003.showLinksOnly = true; Bone_003.showLinks = true; Bone_003.wirecolor = (color 8 110 134); Bone_003.boneEnable=false  -- neck01
-- 			local Bone_004 = BoneSys.createBone (b = [-4.33424e-05,1650,-23.4]) (b + [0,1,0]) d; Bone_004.parent = Bone_003; Bone_004.showLinksOnly = true; Bone_004.showLinks = true; Bone_004.wirecolor = (color 166 202 240); Bone_004.boneEnable=false  -- head
-- 			local Bone_005 = BoneSys.createBone (b = [-45,1465,-34.4001]) (b + [0,1,0]) d; Bone_005.parent = Bone_002; Bone_005.showLinksOnly = true; Bone_005.showLinks = true; Bone_005.wirecolor = (color 6 134 6); Bone_005.boneEnable=false  -- clavicle_r
-- 			local Bone_007 = BoneSys.createBone (b = [-172,1507.83,-34.4001]) (b + [0,1,0]) d; Bone_007.parent = Bone_005; Bone_007.showLinksOnly = true; Bone_007.showLinks = true; Bone_007.wirecolor = (color 6 134 6); Bone_007.boneEnable=false  -- upperarm_r
-- 			local Bone_008 = BoneSys.createBone (b = [-436,1507.83,-34.4001] ) (b + [0,1,0]) d; Bone_008.parent = Bone_007; Bone_008.showLinksOnly = true; Bone_008.showLinks = true; Bone_008.wirecolor = (color 6 134 6); Bone_008.boneEnable=false  -- lowerarm_r
-- 			local Bone_009 = BoneSys.createBone (b = [-590,1507.83,-34.4001]) (b + [0,1,0]) d; Bone_009.parent = Bone_008; Bone_009.showLinksOnly = true; Bone_009.showLinks = true; Bone_009.wirecolor = (color 6 134 6); Bone_009.boneEnable=false  -- wrist_r
-- 			local Bone_010 = BoneSys.createBone (b = [-704,1507.83,-34.4002]) (b + [0,1,0]) d; Bone_010.parent = Bone_009; Bone_010.showLinksOnly = true; Bone_010.showLinks = true; Bone_010.wirecolor = (color 6 134 6); Bone_010.boneEnable=false  -- hand_r
-- 			local Bone_011 = BoneSys.createBone (b = [45,1465,-34.4001]) (b + [0,1,0]) d; Bone_011.parent = Bone_002; Bone_011.showLinksOnly = true; Bone_011.showLinks = true; Bone_011.wirecolor = (color 28 28 177); Bone_011.boneEnable=false  -- clavicle_l
-- 			local Bone_013 = BoneSys.createBone (b = [172,1507.83,-34.4001]) (b + [0,1,0]) d; Bone_013.parent = Bone_011; Bone_013.showLinksOnly = true; Bone_013.showLinks = true; Bone_013.wirecolor = (color 28 28 177); Bone_013.boneEnable=false  -- upperarm_l
-- 			local Bone_014 = BoneSys.createBone (b = [436,1507.83,-34.4001]) (b + [0,1,0]) d; Bone_014.parent = Bone_013; Bone_014.showLinksOnly = true; Bone_014.showLinks = true; Bone_014.wirecolor = (color 28 28 177); Bone_014.boneEnable=false  -- lowerarm_l
-- 			local Bone_015 = BoneSys.createBone (b = [590,1507.83,-34.4001]) (b + [0,1,0]) d; Bone_015.parent = Bone_014; Bone_015.showLinksOnly = true; Bone_015.showLinks = true; Bone_015.wirecolor = (color 28 28 177); Bone_015.boneEnable=false  -- wrist_l
-- 			local Bone_016 = BoneSys.createBone (b = [704,1507.83,-34.4002]) (b + [0,1,0]) d; Bone_016.parent = Bone_015; Bone_016.showLinksOnly = true; Bone_016.showLinks = true; Bone_016.wirecolor = (color 28 28 177); Bone_016.boneEnable=false  -- hand_l
-- 			local Bone_017 = BoneSys.createBone (b = [0,1140,0]) (b + [0,1,0]) d; Bone_017.parent = Bone_000; Bone_017.showLinksOnly = true; Bone_017.showLinks = true; Bone_017.wirecolor = (color 224 198 87); Bone_017.boneEnable=false  -- pelvis
-- 			local Bone_018 = BoneSys.createBone (b = [-92,1010,2.11797e-05]) (b + [0,1,0]) d; Bone_018.parent = Bone_017; Bone_018.showLinksOnly = true; Bone_018.showLinks = true; Bone_018.wirecolor = (color 6 134 6); Bone_018.boneEnable=false  -- thigh_r
-- 			local Bone_019 = BoneSys.createBone (b = [-92,574.51,3.10948e-05]) (b + [0,1,0]) d; Bone_019.parent = Bone_018; Bone_019.showLinksOnly = true; Bone_019.showLinks = true; Bone_019.wirecolor = (color 6 134 6); Bone_019.boneEnable=false  -- calf_r
-- 			local Bone_020 = BoneSys.createBone (b = [-92,135,-4.11142e-06]) (b + [0,1,0]) d; Bone_020.parent = Bone_019; Bone_020.showLinksOnly = true; Bone_020.showLinks = true; Bone_020.wirecolor = (color 6 134 6); Bone_020.boneEnable=false  -- foot_r
-- 			local Bone_022 = BoneSys.createBone (b = [92,1010,2.11797e-05]) (b + [0,1,0]) d; Bone_022.parent = Bone_017; Bone_022.showLinksOnly = true; Bone_022.showLinks = true; Bone_022.wirecolor = (color 28 28 177); Bone_022.boneEnable=false  -- thigh_l
-- 			local Bone_023 = BoneSys.createBone (b = [92,574.51,3.10949e-05]) (b + [0,1,0]) d; Bone_023.parent = Bone_022; Bone_023.showLinksOnly = true; Bone_023.showLinks = true; Bone_023.wirecolor = (color 28 28 177); Bone_023.boneEnable=false  -- calf_l
-- 			local Bone_024 = BoneSys.createBone (b = [92,135,-1.93702e-05]) (b + [0,1,0]) d; Bone_024.parent = Bone_023; Bone_024.showLinksOnly = true; Bone_024.showLinks = true; Bone_024.wirecolor = (color 28 28 177); Bone_024.boneEnable=false  -- foot_l
-- 			local Bone_032 = BoneSys.createBone (b = [-27.88,1725.75,61.59]) (b + [0,1,0]) d; Bone_032.parent = Bone_004; Bone_032.showLinksOnly = true; Bone_032.showLinks = true; Bone_032.wirecolor = (color 224 198 87); Bone_032.boneEnable=false  -- ball_r
-- 			local Bone_033 = BoneSys.createBone (b = [27.8825,1725.75,61.5943]) (b + [0,1,0]) d; Bone_033.parent = Bone_004; Bone_033.showLinksOnly = true; Bone_033.showLinks = true; Bone_033.wirecolor = (color 134 6 6); Bone_033.boneEnable=false  -- ball_l

			Bone_Root.transform = (matrix3 [0.0393701,0,0] [0,0,0.0393701] [0,-0.0393701,0] [0,0,0])
			
			Bone_000.name = "Bone_000"
			Bone_001.name = "Bone_001"
			Bone_002.name = "Bone_002"
			Bone_003.name = "Bone_003"
			Bone_004.name = "Bone_004"
			Bone_005.name = "Bone_005"
			Bone_007.name = "Bone_007"
			Bone_008.name = "Bone_008"
			Bone_009.name = "Bone_009"
			Bone_010.name = "Bone_010"
			Bone_011.name = "Bone_011"
			Bone_013.name = "Bone_013"
			Bone_014.name = "Bone_014"
			Bone_015.name = "Bone_015"
			Bone_016.name = "Bone_016"
			Bone_017.name = "Bone_017"
			Bone_018.name = "Bone_018"
			Bone_019.name = "Bone_019"
			Bone_020.name = "Bone_020"
			Bone_022.name = "Bone_022"
			Bone_023.name = "Bone_023"
			Bone_024.name = "Bone_024"
			Bone_032.name = "Bone_032"
			Bone_033.name = "Bone_033"
			
			idlepose = #(
				(pose_data name:Bone_Root.name matrix:Bone_Root.transform),
				(pose_data name:Bone_000.name matrix:Bone_000.transform),
				(pose_data name:Bone_001.name matrix:Bone_001.transform),
				(pose_data name:Bone_002.name matrix:Bone_002.transform),
				(pose_data name:Bone_003.name matrix:Bone_003.transform),
				(pose_data name:Bone_004.name matrix:Bone_004.transform),
				(pose_data name:Bone_005.name matrix:Bone_005.transform),
				(pose_data name:Bone_007.name matrix:Bone_007.transform),
				(pose_data name:Bone_008.name matrix:Bone_008.transform),
				(pose_data name:Bone_009.name matrix:Bone_009.transform),
				(pose_data name:Bone_010.name matrix:Bone_010.transform),
				(pose_data name:Bone_011.name matrix:Bone_011.transform),
				(pose_data name:Bone_013.name matrix:Bone_013.transform),
				(pose_data name:Bone_014.name matrix:Bone_014.transform),
				(pose_data name:Bone_015.name matrix:Bone_015.transform),
				(pose_data name:Bone_016.name matrix:Bone_016.transform),
				(pose_data name:Bone_017name matrix:Bone_017.transform),
				(pose_data name:Bone_018.name matrix:Bone_018.transform),
				(pose_data name:Bone_019.name matrix:Bone_019.transform),
				(pose_data name:Bone_020.name matrix:Bone_020.transform),
				(pose_data name:Bone_022.name matrix:Bone_022.transform),
				(pose_data name:Bone_023.name matrix:Bone_023.transform),
				(pose_data name:Bone_024.name matrix:Bone_024.transform),
				(pose_data name:Bone_032.name matrix:Bone_032.transform),
				(pose_data name:Bone_033.name matrix:Bone_033.transform)
				)
			)
		)
	fn getIdlePose = (
		local o = undefined
		local sel = selection
		local c = UndefinedClass
		if sel.count == 0 do sel = objects
		idlepose = #()
		for o in sel where (c = classof o) == BoneGeometry or c == Dummy do (
			append idlepose (
				pose_data \
					name:o.name \
					matrix:o.transform
				)
			) 
		if idlepose.count > 0 do messageBox ((idlepose.count as string) + " Transforms Copied")
		)
	fn setIdlePose = (
		if idlepose.count > 0 then (
			local o
			local c = UndefinedClass
			local p = pose_data()
			local a = 0
			for p in idlepose do (
				if (try(o = getNodeByName p.name)catch(undefined)) != undefined do (
					if (c = classOf o) == boneGeometry or c == Dummy do (
						o.transform = p.matrix
						a += 1
						)
					)
				)
			if a > 0 then (
				messageBox ((a as string) + " Transforms Pasted")
				)
			else (
				messageBox "Error:\nNo Transforms Were Applied"
				print idlepose
				)
			) else (messageBox "Error:\nNo Pose Data")
		)
	fn paste_track = (
		if cb_track != undefined and fcv != undefined and lbx1.selection > 0 and lbx1.selection <= fcv.tracks.count do (
			if queryBox "Overwrite Track?" beep:false do (
				local i = 1
				fcv.nodes[lbx1.selection] = cb_track.nodes
				fcv.tracks[lbx1.selection] = cb_track.tracks
				fcv.addrs[lbx1.selection] = cb_track.addrs
				fcv.config[lbx1.selection] = cb_track.config
				for i = 1 to fcv.tracks.count do (
					fcv.tracks[i].index = i
					)
				updateBoneName bname_prefix:(edt_bone_prefix.text as string)
				updateUI()
				updateUI0()
				updateUI2()
				updateUI3()
				updateUI4()
				)
			)
		)
	on ui_fcv open do (
		rcmenu rc_lbx1 (
-- 			menuItem mi_add "Add" checked:false enabled:false
-- 			menuItem mi_remove "Remove" checked:false enabled:false
-- 			separator sep1
			menuItem mi_imp_bin "Load BIN" checked:false
			menuItem mi_imp_fcv "Load FCV" checked:false
			menuItem mi_imp_load_fcv "Load / Apply FCV" checked:false
			separator sep2
			menuItem mi_apply_pose "Apply Pose" checked:false
			menuItem mi_capture_pose "Capture Pose" checked:false
			separator sep3
			menuItem mi_apply_anim "Apply Animation" checked:false
			menuItem mi_exp_anim "Capture Animation" checked:false
			separator sep3b
			menuItem mi_apply_anim_morph "Apply Morph Anim" checked:false
			menuItem mi_exp_anim_morph "Capture Morph Anim" checked:false
			separator sep4
			menuItem mi_build "Build / Apply to PL00" checked:false
			menuItem mi_build_cam "Build / Apply to Cam." checked:false
			separator sep5
			menuItem mi_add "Add" checked:false
			menuItem mi_copy "Copy" checked:false
			menuItem mi_paste "Paste" checked:false
			menuItem mi_del "Delete" checked:false
			on mi_add picked do (add_track())
			on mi_del picked do (erase_track())
			on mi_copy picked do (
				if fcv != undefined and lbx1.selection > 0 and lbx1.selection <= fcv.tracks.count do (
					cb_track = clipboard_track()
					cb_track.nodes = fcv.nodes[lbx1.selection]
					cb_track.tracks = fcv.tracks[lbx1.selection]
					cb_track.addrs = fcv.addrs[lbx1.selection]
					cb_track.config = fcv.config[lbx1.selection]
					)
				)
			on mi_paste picked do (paste_track())
			on mi_capture_pose picked do (if queryBox "Copy Selected Bone Transforms?" beep:false do getIdlePose())
			on mi_apply_pose picked do (setIdlePose())
			on mi_exp_anim picked do (
				if fcv != undefined do (
					with redraw off undo off (
						if chk_exp_old.checked then (
							fcv.getFromScene2 bname_prefix:(edt_bone_prefix.text as string)
							)
						else (
							fcv.getFromScene \
								doRot:chk_exp_rot.checked \
								doPos:chk_exp_pos.checked \
								cullEmpty:chk_exp_cull.checked \
								doMerge:chk_exp_merge.checked \
								mscale:spn_exp_scale.value \
								bname_prefix:(edt_bone_prefix.text as string)
							)
						)
					if lbl_file.caption.count > 0 and lbl_file.caption[lbl_file.caption.count] != "*" do (lbl_file.caption +=  "*")
					lbx1.selection = 1
					updateUI()
					updateUI0()
					updateUI2()
					updateUI3()
					updateUI4()
					)
				)
			on mi_exp_anim_morph picked do (
				if fcv != undefined do (
					with redraw off undo off (
						fcv.getFromScene_morph()
						)
					if lbl_file.caption.count > 0 and lbl_file.caption[lbl_file.caption.count] != "*" do (lbl_file.caption +=  "*")
					lbx1.selection = 1
					updateUI()
					updateUI0()
					updateUI2()
					updateUI3()
					updateUI4()
					)
				)
			on mi_imp_bin picked do (
				with redraw off undo off (
					import_bin_skeleton (
						GetOpenFileName types:"RE4 Binary Mesh File (*.bin)|*.bin|All files (*.*)|*.*|"
						) bname_prefix:(edt_bone_prefix.text as string)
					)
				)
			on mi_imp_fcv picked do (
				if fcv != undefined do (
					read()
					updateUI()
					updateUI0()
					updateUI2()
					updateUI3()
					updateUI4()
					)
				)
			on mi_imp_load_fcv picked do (
				if fcv != undefined do (
					read()
					with redraw off undo "FCV Animation" on (
						fcv.sendToScene bname_prefix:(edt_bone_prefix.text as string)
						)
					updateUI()
					updateUI0()
					updateUI2()
					updateUI3()
					updateUI4()
					)
				)
			on mi_apply_anim picked do (
				if fcv != undefined do (
					with redraw off undo "FCV Animation" on (
						fcv.sendToScene bname_prefix:(edt_bone_prefix.text as string)
						)
					)
				)
			on mi_apply_anim_morph picked do (
				if fcv != undefined do (
					with redraw off undo "FCV Animation" on (
						fcv.sendToScene_morph()
						)
					)
				)
			on mi_build_cam picked do (
				if fcv != undefined do (
					with redraw off undo "FCV Animation" on (
						fcv.sendToScene_camera()
						)
					)
				)
			on mi_build picked do (
				
				if fcv != undefined do (
					if queryBox "Create pl00 Skeleton Example?" beep:false do (
						delete$*
						
						with redraw off undo off (
							build_pl00_skeleton()
							fcv.sendToScene bname_prefix:(edt_bone_prefix.text as string)
							)
						)
					)
				)
			)
		rcmenu rc_lbx2 (
			menuItem mi_add "Add" checked:false
			menuItem mi_remove "Remove" checked:false
			on mi_add picked do (add_key())
			on mi_remove picked do (erase_key())
			)
		if rc_lbx1 != undefined do (registerRightClickMenu rc_lbx1)
		if rc_lbx2 != undefined do (registerRightClickMenu rc_lbx2)
		if fcv == undefined and classOf fmtFCV != UndefinedClass do (fcv = fmtFCV())
		ddl1.items = data_types_list
		ddl2.items = anim_types_list
		edt1.enabled = edt2.enabled = edt3.enabled = edt4.enabled = chk1.checked
		)
	on ui_fcv close do (
		if rc_lbx1 != undefined do (
			unRegisterRightClickMenu rc_lbx1
			rc_lbx1 = undefined
			)
		if rc_lbx2 != undefined do (
			unRegisterRightClickMenu rc_lbx2
			rc_lbx2 = undefined
			)
		)
	on chk_exp_old changed state do (
		if state then (
			chk_exp_rot.enabled = false
			chk_exp_rot.checked = true
			chk_exp_pos.enabled = false
			chk_exp_pos.checked = true
			chk_exp_merge.enabled = false
			chk_exp_merge.checked = false
			chk_exp_cull.enabled = false
			chk_exp_cull.checked = false
			)
		else (
			chk_exp_rot.enabled = chk_exp_rot.enabled = true
			chk_exp_pos.enabled = chk_exp_pos.enabled = true
			chk_exp_merge.enabled = chk_exp_merge.enabled = true
			chk_exp_cull.enabled = chk_exp_cull.enabled = true
			)
		)
	on btn_link_bone pressed do (if queryBox "Add constraints?" beep:false do (with redraw off (skel_const_uhd_to_ue bname_prefix:(edt_bone_prefix.text as string))))
	on btn_sel_const pressed do (with redraw off with undo off (skel_select_const_bones bname_prefix:(edt_bone_prefix.text as string)))
	on btn_export pressed do (
		if chk_exp_old.checked then (
			fcv.getFromScene2 bname_prefix:(edt_bone_prefix.text as string)
			)
		else (
			fcv.getFromScene \
				doRot:chk_exp_rot.checked \
				doPos:chk_exp_pos.checked \
				cullEmpty:chk_exp_cull.checked \
				doMerge:chk_exp_merge.checked \
				mscale:spn_exp_scale.value \
				bname_prefix:(edt_bone_prefix.text as string)
			)
		if lbl_file.caption.count > 0 and lbl_file.caption[lbl_file.caption.count] != "*" do (lbl_file.caption +=  "*")
		lbx1.selection = 1
		updateUI()
		updateUI0()
		updateUI2()
		updateUI3()
		updateUI4()
		)
	on btn_idle_pose pressed do (setIdlePose())
	on edt1 entered txt do (updateUI4_Value())
	on edt2 entered txt do (updateUI4_Value())
	on edt3 entered txt do (updateUI4_Value())
	on edt4 entered txt do (updateUI4_Value())
	on spn3 changed val do (
		if val > spn1.value do (spn1.value = val)
		updateUI3_Value()
		)
	on spn1 changed val do (fcv.num_frames = val)
	on spn2 changed val do (
		if lbx1.selection > 0 and lbx1.selection <= fcv.nodes.count do (
			fcv.nodes[lbx1.selection] = val
			)
		updateBoneName bname_prefix:(edt_bone_prefix.text as string)
		updateUI()
		)
	on btn1 pressed do (
		read()
		updateUI()
		updateUI0()
		updateUI2()
		updateUI3()
		updateUI4()
		)
	on btn2 pressed do (write())
	on btn_t_add pressed do (add_track())
	on btn_t_sub pressed do (erase_track())
	on btn_k_add pressed do (add_key())
	on btn_k_sub pressed do (erase_key())
	on ddl1 selected sel do (
		if lbx1.selection > 0 and lbx1.selection <= fcv.tracks.count do (
			fcv.config[lbx1.selection].data = data_types[sel]
			)
		)
	on ddl2 selected sel do (
		if lbx1.selection > 0 and lbx1.selection <= fcv.tracks.count do (
			fcv.config[lbx1.selection].type = anim_types[sel]
			updateBoneName bname_prefix:(edt_bone_prefix.text as string)
			updateUI()
			)
		)
	on lbx1 selected sel do (
		updateUI0()
		updateUI2()
		updateUI3()
		updateUI4()
		)
	on lbx1 rightClick do (if rc_lbx1 != undefined do popUpMenu rc_lbx1 align:#align_topleft)
	on lbx2 selected sel do (
		updateUI3()
		updateUI4()
		)
	on lbx2 rightClick do (if rc_lbx2 != undefined do popUpMenu rc_lbx2 align:#align_topleft)
	on lbx3 selected sel do (
		updateUI4()
		)
	on chk1 changed state do (
		edt1.enabled = edt2.enabled = edt3.enabled = edt4.enabled = chk1.checked
		updateUI4_Enable(state)
		)
	)
createDialog ui_fcv