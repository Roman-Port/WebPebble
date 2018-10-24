﻿def FlagsForFile(filename, **kwargs):
	return {{
		'flags': [
			'-std=c11',
			'-x',
			'c',
			'-Wall',
			'-Wextra',
			'-Werror',
			'-Wno-unused-parameter',
			'-Wno-error=unused-function',
			'-Wno-error=unused-variable',
			'-I{sdk}/pebble/basalt/include',
			'-I{here}/build',
			'-I{here}',
			'-I{here}/build/src',
			'-I{here}/src',
			'-isystem',
			'{stdlib}',
			'-DRELEASE',
			'-DPBL_PLATFORM_BASALT',
			'-DPBL_COLOR',
			'-DPBL_SDK_3',
			'-DPBL_RECT',
			'-D_TIME_H_',
		],
		'do_cache': True,
	}}