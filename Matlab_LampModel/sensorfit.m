% fit light sensor model
% Zhen Song
% Sep. 25 2006

% % 35W halogan light bulb
% Brightness=[7079.1
% 6160.9
% 5200
% 4500
% 3786.4
% 3696.2
% 3127.708333
% 3001.55
% 2900
% 2900];
% w=[1 1 1 2 5 2 1 1 1 1]';


% 30W flouriscent light 
Brightness=[6572.2
5395.1
4200
3289.2
2872.45
2629.65
2549.3
2524.9];
% Brightness=[6572.2
% 5395.1
% 3643.9
% 3289.2
% 2872.45
% 2629.65
% 2549.3
% 2524.9];
w=[1 1 1 2 2 2 1 1 ]';

DistanceInCM=[12.7
25.4
38.1
50.8
63.5
76.2
88.9
101.6];


cinit=[7000;20];
% try: add additional fake data
ln=[0:12.7:220]';
AugBright=[6572; Brightness; 2500*ones(length(ln),1)];
AugDist=[0; DistanceInCM; 101+[0:12.7:220]'];
[c, resnorm, residual] = lsqcurvefit(@SensorModel,cinit,AugDist,AugBright);
c

cinit2=[70;0.1];
[c2, resnorm, residual] = lsqcurvefit(@SensorModel,cinit2,DistanceInCM,Brightness);

Bw=sqrt(w).*Brightness;
[cw,rw,Jw]=nlinfit(DistanceInCM, Bw, @SensorModel, cinit);

dist=linspace(0,150);
bright=c(1)*exp(-dist.^2/c(2));
h1=plot(dist,bright);
hold on;

% bright2=c(1)*(dist/c(2)).^(-2);
% h2=plot(dist,bright2,'-.');

plot(DistanceInCM, Brightness,'r+-');

bright3=cw(1)*exp(-dist.^2/cw(2));
h3=plot(dist,bright3,'k-');

p=polyfit(DistanceInCM,Brightness,2);
d=dist';
% bright4=[d.^3 d.^2 d ones(size(d))]* p';
bright4=[ d.^2 d ones(size(d))]* p';
h4=plot(dist, bright4,'k-.');

% legend([h1,h2],'exp model','x^-2 model');
axis([0 250 0 8000]);