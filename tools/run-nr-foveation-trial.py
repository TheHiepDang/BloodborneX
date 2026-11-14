"""Launch only an explicitly prepared isolated M2-B trial; never update an installation."""
from pathlib import Path
import argparse,datetime,hashlib,json,os,subprocess,sys,time,ctypes
from ctypes import wintypes
p=argparse.ArgumentParser(description=__doc__)
p.add_argument('--trial',type=Path,required=True)
p.add_argument('--mode',choices=['standard','foveated','fault-create','fault-evaluate'],required=True)
p.add_argument('--run-id',required=True)
p.add_argument('--validation-layer',type=Path)
p.add_argument('--capture',action='store_true')
p.add_argument('--seconds',type=int,default=0,help='0: user closes game; otherwise bounded owned-process trial')
a=p.parse_args();trial=a.trial.resolve()
if not a.run_id or any(c not in 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_' for c in a.run_id):p.error('run-id must be a simple unique filename component')
m=json.loads((trial/'TRIAL-MANIFEST.json').read_text())
if m.get('developer_trial_only') is not True:p.error('Not an isolated developer trial')
source=Path(m['native_source']);run=trial/'logs'/a.run_id;run.mkdir(exist_ok=False)
normal=m.get('normal_bloodbornepc_workflow') is True
binaries=trial/'engine/out' if normal else trial/'out'
config=(trial/'lantern.ini').read_text(encoding='utf-8-sig')
settings=dict(line.strip().split('=',1) for line in config.splitlines() if '=' in line and not line.lstrip().startswith(('#',';')))
if settings.get('dlss_nr_passes')!='1' or settings.get('dlss_nr_composition')!='0':p.error('M2-B requires one-pass Legacy configuration')
if settings.get('frame_gen')!='off' or settings.get('online_enabled')!='0':p.error('Prepared trial requires FG off and online disabled')
for key in ['bb-probe.exe','nvngx.dll_dlssnr.dll','bb-engine-assets.exe','bb-shader-lab.exe']:
 if not (binaries/key).is_file():p.error('Missing fresh trial output: '+key)
env={k:v for k,v in os.environ.items() if not k.startswith('BB_')}
env.update(BB_PREBUILT='1',BB_DATA_DIR=str(trial),BB_CONFIG=str(trial/'lantern.ini'),BB_USER_DIR=str(trial/'user'),
 BB_GPU_USER_DIR=str(trial/'user'),BB_PROBE=str(binaries/'bb-probe.exe'),BB_DLSS_DIR=m['model_dir'],
 BB_MODS_ENABLED='0',BB_ONLINE_PAUSED='1',BB_DLSS_NR_PROFILE='1',BB_DLSS_NR_PROFILE_CSV=str(run/'nr.csv'),
 BB_LOG_FILE=str(run/'session.log'),BB_FPS='uncap',BB_FPS_LIMIT='0',BB_VBLANK_HZ='0',BB_CAP_TO_REFRESH='0',
 TEMP=str(trial/'temp'),TMP=str(trial/'temp'),APPDATA=str(trial/'windows/AppData/Roaming'),
 LOCALAPPDATA=str(trial/'windows/AppData/Local'),VK_LOADER_LAYERS_DISABLE='~implicit~')
for key in ['VK_INSTANCE_LAYERS','VK_LAYER_PATH','VK_LAYER_VALIDATE_SYNC','VK_LAYER_ENABLES']:env.pop(key,None)
if normal:
 env['BB_LAUNCHER_CONFIG_DIR']=str(trial/'developer-profile')
 env.pop('BB_PROBE',None)
 # Let the normal launcher and its bundled Python establish the engine's
 # environment. The earlier direct trial's AppData remapping is unnecessary.
 for key in ['APPDATA','LOCALAPPDATA']:
  if key in os.environ:env[key]=os.environ[key]
env['BB_DLSS_NR_FOVEATION_POC']='0' if a.mode=='standard' else '1'
if a.mode.startswith('fault-'):env['BB_DLSS_NR_FOVEATION_FAULT']=a.mode.removeprefix('fault-')
if a.capture:env['BB_DLSS_NR_CAPTURE']=str(trial/'captures'/a.run_id)
if a.validation_layer:
 layer=a.validation_layer.resolve()
 if not (layer/'VkLayer_khronos_validation.json').is_file():p.error('Missing validation manifest')
 env.update(VK_LAYER_PATH=str(layer),VK_INSTANCE_LAYERS='VK_LAYER_KHRONOS_validation',VK_LAYER_VALIDATE_SYNC='1')
 env['PATH']=str(layer)+';'+env.get('PATH','')
cmd=[str(trial/'Lantern Engine.exe'),'--play'] if normal else [sys.executable,str(source/'scripts/run_windows.py'),'--game-dir',m['game_dir']]
receipt={'mode':a.mode,'run_id':a.run_id,'validation_requested':bool(a.validation_layer),'start_utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),
 'timings_are_not_gameplay_certification':True,'command':cmd,'config':str(trial/'lantern.ini'),
 'config_sha256':hashlib.sha256((trial/'lantern.ini').read_bytes()).hexdigest(),
 'executable_sha256':hashlib.sha256((binaries/'bb-probe.exe').read_bytes()).hexdigest(),
 'normal_bloodbornepc_workflow':normal}
(run/'run.json').write_text(json.dumps(receipt,indent=2)+'\n')
with (run/'launcher.log').open('w') as log:
 # A private kill-on-close job owns the suspended launcher before it can spawn
 # the game. This avoids taskkill credentials/process-enumeration dependencies.
 class BasicLimit(ctypes.Structure):
  _fields_=[('process_time',ctypes.c_longlong),('job_time',ctypes.c_longlong),('flags',wintypes.DWORD),
   ('min_working',ctypes.c_size_t),('max_working',ctypes.c_size_t),('active',wintypes.DWORD),
   ('affinity',ctypes.c_size_t),('priority',wintypes.DWORD),('scheduling',wintypes.DWORD)]
 class Limits(ctypes.Structure):
  _fields_=[('basic',BasicLimit),('io',ctypes.c_ulonglong*6),('process_memory',ctypes.c_size_t),
   ('job_memory',ctypes.c_size_t),('peak_process',ctypes.c_size_t),('peak_job',ctypes.c_size_t)]
 kernel=ctypes.WinDLL('kernel32',use_last_error=True)
 kernel.CreateJobObjectW.argtypes=[ctypes.c_void_p,wintypes.LPCWSTR];kernel.CreateJobObjectW.restype=wintypes.HANDLE
 kernel.SetInformationJobObject.argtypes=[wintypes.HANDLE,ctypes.c_int,ctypes.c_void_p,wintypes.DWORD]
 kernel.AssignProcessToJobObject.argtypes=[wintypes.HANDLE,wintypes.HANDLE]
 kernel.TerminateJobObject.argtypes=[wintypes.HANDLE,wintypes.UINT];kernel.CloseHandle.argtypes=[wintypes.HANDLE]
 job=kernel.CreateJobObjectW(None,None)
 if not job:raise ctypes.WinError(ctypes.get_last_error())
 limits=Limits();limits.basic.flags=0x2000
 if not kernel.SetInformationJobObject(job,9,ctypes.byref(limits),ctypes.sizeof(limits)):
  error=ctypes.get_last_error();kernel.CloseHandle(job);raise ctypes.WinError(error)
 proc=subprocess.Popen(cmd,cwd=source,env=env,stdout=log,stderr=subprocess.STDOUT,creationflags=0x4)
 if not kernel.AssignProcessToJobObject(job,wintypes.HANDLE(int(proc._handle))):
  error=ctypes.get_last_error();proc.kill();proc.wait();kernel.CloseHandle(job);raise ctypes.WinError(error)
 resume=ctypes.WinDLL('ntdll').NtResumeProcess;resume.argtypes=[wintypes.HANDLE];resume.restype=wintypes.LONG
 if resume(wintypes.HANDLE(int(proc._handle)))!=0:
  kernel.CloseHandle(job);proc.wait();raise RuntimeError('Unable to resume owned trial launcher')
 receipt['launcher_pid']=proc.pid
 try:
  receipt['launcher_exit']=proc.wait(timeout=a.seconds if a.seconds>0 else None)
 except subprocess.TimeoutExpired:
  # Terminate only this owned launcher and its descendants, whose saves/config are isolated.
  receipt['bounded_trial_timeout']=True
  receipt['owned_job_cleanup_ok']=bool(kernel.TerminateJobObject(job,0))
  proc.wait(timeout=15)
 finally:
  kernel.CloseHandle(job)
  receipt['end_utc']=datetime.datetime.now(datetime.timezone.utc).isoformat()
  csv=run/'nr.csv'
  receipt['nr_samples']=max(0,len(csv.read_text().splitlines())-1) if csv.is_file() else 0
  (run/'run.json').write_text(json.dumps(receipt,indent=2)+'\n')
print(json.dumps(receipt,indent=2))
sys.exit(receipt.get('launcher_exit',0))
