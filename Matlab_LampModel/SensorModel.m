function bright=SensorModel(C,dist)
bright = C(1)*exp(-dist.^2/C(2));
% bright = C(1)*exp(-dist.^2);