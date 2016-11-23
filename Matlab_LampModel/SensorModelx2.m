function bright=SensorModelx2(C,dist)
bright = C(1)*(dist/C(2)).^(-2);
% bright = C(1)*exp(-dist.^2);