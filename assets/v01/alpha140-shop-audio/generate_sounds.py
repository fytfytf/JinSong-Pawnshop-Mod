"""Original, deterministic short procedural effects; no sampled or downloaded audio."""
import hashlib, json, math, random, re, shutil, struct, wave
from pathlib import Path

folder = Path(__file__).resolve().parent
root = folder.parents[2]
source = (root / 'src/ShopAudio.cs').read_text(encoding='utf-8')
keys = list(dict.fromkeys(re.findall(r'new\[\] \{ "[^"]+", "[^"]+", "([^"]+)" \}', source)))
rate = 22050
records = []
for ordinal, key in enumerate(keys):
    rng = random.Random(14000 + ordinal)
    kind = 'wood'
    duration = .09
    if any(w in key for w in ('paper', 'page', 'writing')): kind, duration = 'paper', .22
    elif 'book' in key: kind, duration = 'cloth', .20
    elif 'cloth' in key: kind, duration = 'cloth', .42
    elif 'step' in key: kind, duration = 'step', .18
    elif 'coin' in key or 'register' in key: kind, duration = 'metal', .32
    elif 'alarm' in key: kind, duration = 'bell', .72
    elif any(w in key for w in ('negative', 'fail', 'cross')): kind, duration = 'negative', .25
    elif 'door' in key: kind, duration = 'door', .45
    elif 'water' in key: kind, duration = 'water', .65
    elif any(w in key for w in ('ratchet', 'wire', 'welder')): kind, duration = 'mechanism', .28
    values = []
    low = 0.0
    for i in range(round(duration * rate)):
        t = i / rate
        noise = rng.uniform(-1, 1)
        low = .75 * low + .25 * noise
        if kind == 'wood':
            freq = 530 + 95 * (ordinal % 4)
            v = (math.sin(2*math.pi*freq*t) + .4*math.sin(2*math.pi*1.72*freq*t) + .45*noise) * math.exp(-t*65)
        elif kind in ('paper', 'cloth', 'step'):
            env = math.sin(math.pi*t/duration)**1.4
            v = (noise-low if kind == 'paper' else low) * env
            if kind == 'step': v += .7*math.sin(2*math.pi*115*t)*math.exp(-t*45)
        elif kind in ('metal', 'bell'):
            v = 0
            for onset in ([0,.08,.16] if kind=='metal' else [0,.23,.46]):
                u=t-onset
                if u>=0:
                    f=1250 if kind=='metal' else 790
                    v += (.7*math.sin(2*math.pi*f*u)+.24*math.sin(2*math.pi*f*1.47*u))*math.exp(-u*22)
        elif kind=='negative':
            v=sum(.7*math.sin(2*math.pi*310*(t-o))*math.exp(-(t-o)*42) for o in [0,.12] if t>=o)
        elif kind=='water':
            v=(low+.12*math.sin(2*math.pi*(1300*t+80*t*t)))*math.sin(math.pi*t/duration)
        elif kind=='door':
            v=.6*low*math.sin(math.pi*t/duration)+math.sin(2*math.pi*140*t)*math.exp(-t*30)
        else:
            v=low*math.sin(math.pi*t/duration)+.12*math.sin(2*math.pi*430*t)*(1+math.sin(2*math.pi*35*t))*math.exp(-t*8)
        fade=min(1,t/.003,(duration-t)/.006)
        values.append(v*max(0,fade))
    scale=.30/max(abs(v) for v in values)
    samples=[round(v*scale*32767) for v in values]
    out=folder/'Sounds'/('jin_'+key+'.wav')
    out.parent.mkdir(exist_ok=True)
    with wave.open(str(out),'wb') as audio:
        audio.setnchannels(1);audio.setsampwidth(2);audio.setframerate(rate)
        audio.writeframes(struct.pack('<'+'h'*len(samples),*samples))
    stage=root/'dist/jinsong_poc/Sounds'/out.name
    stage.parent.mkdir(exist_ok=True);shutil.copy2(out,stage)
    records.append(dict(identifier=key,file=out.name,kind=kind,sample_rate=rate,channels=1,frames=len(samples),seconds=len(samples)/rate,peak_pcm=max(abs(v) for v in samples),sha256=hashlib.sha256(out.read_bytes()).hexdigest(),origin='Original deterministic procedural synthesis, no source samples'))
(folder/'sounds.json').write_text(json.dumps(records,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
(folder/'batch.json').write_text('[]\n',encoding='utf-8')
print(json.dumps({'outcome':'passed','created_audio_files':len(records),'max_duration':max(r['seconds'] for r in records),'source':'Original procedural synthesis; no listening acceptance claim'}))
