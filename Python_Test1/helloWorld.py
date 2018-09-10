import json
from linq import Flow, extension_std

#seq = Flow(range(100))
#res = seq.Zip(range(100, 200)).Map(lambda fst, snd : fst/snd).GroupBy(lambda num: num//0.2).Unboxed()

deviceInfosString = open('C:/Projects/TestProjects/TestData_Python/deviceInfo.txt')
deviceDescriptionsString = open('C:/Projects/TestProjects/TestData_Python/deviceDescription.txt')

#print('------------------------------------------------')
#parseableObject['deviceInfos'].select(lambda x: x['type'] == 'CMCIII-PU').Each(lambda y: print(y))
#print('################################################')
#for element in parseableObject['deviceInfos']:
#    print(element)


deviceDescriptions = json.loads(deviceDescriptionsString.read(-1),encoding='Utf-8')
deviceInfos = json.loads(deviceInfosString.read(-1))

#parseableObject = json.loads(deviceInfos)

print((deviceInfos['result'] == 0) & (deviceInfos['deviceInfos'][0]['info']['type'] == 'CMCIII-PU') & (deviceInfos['deviceInfos'][0]['info']['orderNumber'] == '7030.000'))
print([x for x in deviceInfos['deviceInfos'] if x['info']['type'] == 'CMCIII-PU'])
# if x[0]['info']['type'] == 'CMCIII-PU'

def Match(jsonString):
    "matches specific device"



    return False

#print([x for x in parseableObject['deviceInfos'] if x['info']['type'] == 'CMCIII-PU'] )

name = 'foo'
st ='bar'

inter = f'This is {name} or {st}'

print(inter)

#print(parseableObject)