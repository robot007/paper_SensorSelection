% LampModelFit
%   Fit lamp model by experiment data.
%   Lamp: a 35w Halogen lamp, height 8 in (20.32 cm).
%   Sensor: Telos Sky Mote (Tmote sky)
%   http://www.moteiv.com/products/tmotesky.php .
%   Ambient light value: 2000 measured by Tmote.
%   Note: this model is used by sensor selection demo.
%   Comments on data: 
%       dist: the distance from the sensor to the point right under the
%       lamp.
%          
%         lamp
%         |   \
%         |    \
%         |     \
%  height |      \
%         |       \
%         |__dist__sensor
% 
% 
% 
%       dat: experiment data. The columns are ordered by distance. The raws
%       are ordered by trials. 


% Zhen Song
% Feb. 2007

close all;
clear all;

% dat: column is range, raw is trial number
dat=[6799,	5773,	4901,	3900,	3356,	2758,	2459,	2423,	2032,	2160,	2178;...
6750,	5639,	4797,	3967,	3350,	2783,	2459,	2416,	2056,	2166,	2185;...
6738,	5780,	4919,	3918,	3363,	2795,	2478,	2416,	2026,	2148,	2197;...
6787,	5749,	4852,	3887,	3381,	2777,	2441,	2429,	2020,	2142,	2215;...
6805,	5639,	4772,	3918,	3405,	2770,	2447,	2429,	2026,	2166,	2178;...
6768,	5798,	4852,	3979,	3375,	2819,	2484,	2404,	2020,	2166,	2191;...
6750,	5755,	4931,	3967,	3344,	2764,	2484,	2404,	2032,	2166,	2191;...
6787,	5651,	4797,	3900,	3332,	2795,	2465,	2404,	2038,	2142,	2172;...
6744,	5682,	4797,	3881,	3356,	2801,	2465,	2435,	2020,	2142,	2178;...
6726,	5798,	4901,	3912,	3399,	2777,	2459,	2410,	2038,	2166,	2203;...
6750,	5670,	4772,	3967,	3344,	2783,	2484,	2410,	2020,	2166,	2191;...
6781,	5651,	4840,	3955,	3363,	2789,	2459,	2416,	2020,	2142,	2191;...
6762,	5755,	4925,	3869,	3369,	2764,	2478,	2429,	2038,	2136,	2209;...
6726,	5798,	4846,	3881,	3417,	2789,	2465,	2410,	2050,	2172,	2209;...
6726,	5712,	4766,	3918,	3399,	2795,	2459,	2410,	2026,	2160,	2203;...
6750,	5682,	4888,	3973,	3375,	2770,	2453,	2423,	2020,	2172,	2209;...
6707,	5767,	4791,	3942,	3350,	2801,	2496,	2410,	2050,	2154,	2209;...
6732,	5780,	4797,	3894,	3363,	2801,	2478,	2435,	2056,	2178,	2197;...
6756,	5694,	4925,	3875,	3405,	2777,	2447,	2410,	2044,	2142,	2185;...
6774,	5633,	4888,	3948,	3399,	2783,	2453,	2416,	2026,	2154,	2203;...
6707,	5712,	4785,	3967,	3369,	2770,	2465,	2423,	2044,	2154,	2191;...
6732,	5798,	4919,	3918,	3356,	2801,	2465,	2423,	2056,	2160,	2185;...
6781,	5780,	4779,	3857,	3369,	2764,	2484,	2423,	2062,	2172,	2197;...
6774,	5676,	4815,	3875,	3411,	2770,	2471,	2423,	2044,	2148,	2178;...
6726,	5651,	4901,	3936,	3405,	2795,	2453,	2398,	2050,	2172,	2197;...
6738,	5706,	4797,	3955,	3363,	2770,	2465,	2423,	2032,	2166,	2215;...
6756,	5737,	4797,	3912,	3363,	2783,	2459,	2404,	2026,	2160,	2197;...
6719,	5639,	4882,	3851,	3405,	2770,	2465,	2435,	2026,	2166,	2203;...
6713,	5633,	4919,	3875,	3417,	2770,	2465,	2429,	2044,	2142,	2209;...
6774,	5731,	4821,	3900,	3393,	2807,	2453,	2392,	2056,	2142,	2209;...
6719,	5786,	4840,	3948,	3363,	2795,	2447,	2416,	2044,	2160,	2197;...
6707,	5609,	4754,	3887,	3405,	2801,	2496,	2410,	2026,	2148,	2215;...
6750,	5682,	4833,	3869,	3387,	2758,	2484,	2404,	2062,	2160,	2191;...
6774,	5786,	4846,	3918,	3363,	2777,	2478,	2404,	2056,	2148,	2197;...
6726,	5761,	4803,	3961,	3375,	2807,	2484,	2386,	2044,	2172,	2209;...
6701,	5645,	4919,	3948,	3411,	2770,	2484,	2423,	2056,	2166,	2197;...
6719,	5633,	4901,	3900,	3387,	2783,	2465,	2429,	2032,	2172,	2203;...
6756,	5798,	4876,	3875,	3332,	2770,	2453,	2423,	2056,	2148,	2185;...
6744,	5633,	4925,	3912,	3399,	2770,	2465,	2404,	2038,	2185,	2191;...
6689,	5700,	4809,	3961,	3405,	2770,	2459,	2416,	2038,	2154,	2215;...
6707,	5810,	4760,	3924,	3356,	2764,	2465,	2398,	2044,	2172,	2215;...
6756,	5731,	4827,	3869,	3363,	2795,	2441,	2404,	2050,	2148,	2185;...
6750,	5798,	4919,	3851,	3405,	2795,	2447,	2416,	2026,	2178,	2209;...
6707,	5737,	4888,	3900,	3430,	2795,	2478,	2404,	2050,	2166,	2209;...
6701,	5633,	4785,	3924,	3375,	2795,	2459,	2386,	2044,	2154,	2191;...
6750,	5682,	4785,	3948,	3363,	2770,	2471,	2429,	2056,	2148,	2191;...
6713,	5780,	4913,	3930,	3411,	2789,	2441,	2410,	2062,	2166,	2185;...
6695,	5633,	4901,	3881,	3399,	2801,	2478,	2404,	2056,	2178,	2215;...
6726,	5670,	4791,	3863,	3338,	2764,	2478,	2410,	2038,	2160,	2209;...
6726,	5773,	4913,	3869,	3375,	2807,	2490,	2416,	2044,	2166,	2215;...
6695,	5657,	4901,	3894,	3411,	2795,	2441,	2410,	2050,	2166,	2221;...
6707,	5633,	4797,	3930,	3399,	2758,	2484,	2410,	2038,	2154,	2185;...
6750,	5725,	4803,	3961,	3363,	2783,	2484,	2410,	2050,	2172,	2209;...
6726,	5773,	4925,	3900,	3350,	2777,	2465,	2398,	2050,	2154,	2209;...
6713,	5725,	4901,	3863,	3417,	2777,	2478,	2398,	2032,	2160,	2203;...
6756,	5639,	4797,	3887,	3405,	2770,	2453,	2435,	2038,	2160,	2215;...
6707,	5725,	4901,	3930,	3338,	2801,	2478,	2398,	2062,	2160,	2191;...
6683,	5786,	4797,	3948,	3399,	2770,	2471,	2398,	2038,	2166,	2209;...
6726,	5700,	4772,	3912,	3405,	2807,	2471,	2386,	2032,	2185,	2197;...
6744,	5590,	4901,	3845,	3356,	2801,	2465,	2429,	2050,	2172,	2191;...
6701,	5615,	4901,	3881,	3363,	2795,	2459,	2392,	2056,	2166,	2215;...
6726,	5700,	4791,	3900,	3393,	2758,	2484,	2404,	2038,	2178,	2191;...
6750,	5761,	4882,	3955,	3350,	2789,	2459,	2398,	2044,	2172,	2209;...
6689,	5731,	4895,	3948,	3387,	2770,	2459,	2410,	2044,	2166,	2209;...
6689,	5633,	4797,	3875,	3417,	2807,	2471,	2404,	2062,	2178,	2209;...
6732,	5657,	4785,	3839,	3381,	2795,	2459,	2404,	2069,	2185,	2221;...
6701,	5761,	4901,	3851,	3393,	2832,	2465,	2404,	2038,	2166,	2221;...
6689,	5786,	4760,	3936,	3424,	2801,	2478,	2410,	2056,	2178,	2197;...
6726,	5688,	4772,	3900,	3393,	2801,	2465,	2386,	2062,	2185,	2203;...
6713,	5609,	4870,	3851,	3350,	2777,	2496,	2410,	2038,	2185,	2203;...
6677,	5633,	4901,	3863,	3363,	2777,	2484,	2404,	2032,	2166,	2215;...
6701,	5712,	4766,	3894,	3411,	2819,	2465,	2423,	2056,	2160,	2227;...
6738,	5792,	4901,	3924,	3350,	2825,	2453,	2386,	2050,	2185,	2203;...
6738,	5731,	4809,	3863,	3356,	2770,	2459,	2410,	2056,	2166,	2191;...
6701,	5651,	4772,	3851,	3381,	2801,	2478,	2392,	2044,	2154,	2209;...
6671,	5621,	4913,	3900,	3430,	2795,	2453,	2410,	2056,	2178,	2215;...
6719,	5706,	4797,	3948,	3387,	2783,	2453,	2429,	2062,	2185,	2215;...
6744,	5633,	4913,	3875,	3363,	2777,	2471,	2416,	2038,	2191,	2203;...
6707,	5645,	4870,	3900,	3350,	2807,	2459,	2392,	2032,	2166,	2203;...
6671,	5657,	4779,	3948,	3411,	2770,	2441,	2429,	2056,	2160,	2221;...
6707,	5633,	4827,	3948,	3369,	2770,	2447,	2410,	2056,	2166,	2197;...
6738,	5676,	4919,	3863,	3332,	2783,	2465,	2410,	2044,	2178,	2227;...
6701,	5798,	4809,	3948,	3375,	2783,	2484,	2404,	2038,	2166,	2227;...
6665,	5627,	4791,	3942,	3399,	2764,	2478,	2392,	2038,	2172,	2209;...
6707,	5621,	4888,	3875,	3411,	2764,	2465,	2410,	2044,	2160,	2227;...
6744,	5712,	4766,	3845,	3369,	2819,	2490,	2404,	2038,	2166,	2227;...
6707,	5761,	4840,	3918,	3375,	2813,	2459,	2410,	2044,	2185,	2221;...
6677,	5609,	4919,	3967,	3405,	2795,	2459,	2410,	2069,	2191,	2221;...
6726,	5657,	4821,	3936,	3356,	2807,	2484,	2392,	2069,	2178,	2209;...
6744,	5731,	4779,	3845,	3399,	2813,	2453,	2404,	2032,	2172,	2215;...
6683,	5798,	4876,	3851,	3405,	2783,	2465,	2429,	2069,	2160,	2197;...
6683,	5755,	4913,	3918,	3405,	2777,	2471,	2404,	2038,	2185,	2233;...
6738,	5670,	4803,	3948,	3338,	2795,	2459,	2410,	2038,	2191,	2209;...
6677,	5609,	4913,	3851,	3375,	2807,	2459,	2404,	2056,	2197,	2221;...
6701,	5645,	4779,	3900,	3430,	2813,	2471,	2410,	2069,	2166,	2197;...
6732,	5725,	4840,	3967,	3381,	2795,	2465,	2416,	2056,	2178,	2233;...
6677,	5767,	4943,	3912,	3356,	2777,	2471,	2410,	2044,	2172,	2215;...
6695,	5700,	4864,	3845,	3381,	2783,	2471,	2404,	2075,	2191,	2233;...
6744,	5621,	4772,	3887,	3411,	2801,	2465,	2392,	2044,	2185,	2233;...
6726,	5627,	4858,	3942,	3399,	2801,	2478,	2410,	2050,	2185,	2227];

dist=[0	5.08	10.16	15.24	20.32	25.4	30.48	35.56	40.64	45.72	50.8]; % cm
rangestd=std(dat,0,1);
rangelight=mean(dat,1);

plot(dist, rangelight,'*', 'MarkerSize',9);
hold on;
plot(dist, rangelight,'LineWidth',2);

%         public double[] BrightCurve ={ 6720.0, -216.0, 2.524 }; // quadratic fit
%         // public double[] StdCurve ={ 82.4, -3.626, 0.04577 };// robust quadratic fit. This is the Std for 100 samples, not 1 nominal sample
%         public double[] StdCurve ={ 824.0, -36.26, 0.4577 };// robust quadratic fit

figure(1)
%  cv_ = {2.523653896375, -215.9941913074, 6719.678601399};
RangeLightMatv6(dist,rangelight)
% print -deps2c RangeLightFit
% saveas(gcf,'RangeLightFit','fig');

figure(2)
%    cv_ = {0.04636205804747, -3.679329939961, 83.49908233326};
d2=dist(2:end);
s2=rangestd(2:end);
RangeStdMatv6(d2,s2);
% print -deps2c RangeStdFit
% saveas(gcf,'RangeStdFit','fig');

figure(3);
myd=linspace(0,60);
mydd=myd(1:end-1);
cLt=[2.523653896375, -215.9941913074, 6719.678601399];
cStd=[0.04636205804747, -3.679329939961, 83.49908233326];
ltdist=cLt*[myd.^2; myd; ones(1,length(myd))];
stddist=cStd*[myd.^2; myd; ones(1,length(myd))];
difltdist=diff(ltdist);
dltstddist=difltdist./stddist(1:end-1);
h1=plot(mydd,dltstddist,'LineWidth',3);
xlabel('Distance (cm)');
ylabel('Sensitivity/std');
grid on;
title('Sensitivity over stand deviation.');
% print -deps2c SensitivityDistFit
% saveas(gcf,'SensitivityDistFit','fig');

figure(4);
h1=plot(mydd,dltstddist.^2,'LineWidth',3);
xlabel('Distance (cm)');
ylabel('(Sensitivity/std)^2');
grid on;
title('The square of sensitivity over stand deviation.');
% print -deps2c QuadSensitivityDistFit
% saveas(gcf,'QuadSensitivityDistFit','fig');

figure(5);
h1=plot(myd,ltdist,'LineWidth',2);
hold on;
h2=plot(myd,ltdist+3*stddist, ':', 'LineWidth',2);
plot(myd,ltdist-3*stddist, ':', 'LineWidth',2);
legend([h1,h2],'Light','3 \sigma error bound');
xlabel('Distance (cm)');
ylabel('Sensitivity/std');
grid on;
title('Light curve with error bound.');
% print -deps2c RangeStdBnd
% saveas(gcf,'RangeStdBnd','fig');

figure(6)
% show that the sensor is uncorrelated (tempero-spatial)
R=corrcoef(dat);
surf(dist',dist,R);
xlabel('Distance (cm)'); 
ylabel('Distance (cm)'); 
zlabel('Corr. coef.');
title('The correlation coefficients of Tmote Sky light sensors.'); 
campos([-20, -40, 4]);
% print -deps2c RangeStdBnd
% saveas(gcf,'RangeStdBnd','fig');

figure(7)
% show that the cov matrix should be diagonal
Mcov=cov(dat);
surf(dist',dist,Mcov);
xlabel('Distance (cm)'); 
ylabel('Distance (cm)'); 
zlabel('Covariance');
title('The covariance of Tmote Sky light sensors.'); 
campos([60, 90, 6000]);

figure(8)
% show that the sensor noise is not gaussian
Alln=[]; Allx=[];
Len=size(dat,2);
for cnt=1:Len
    [n, x]=hist(dat(:,cnt));
    Alln=[Alln; n];
    % shift
    x=x-mean(x);
    Allx=[Allx; x];
end
% Alln=[zeros(Len,1) Alln zeros(Len,1)];
% Allx=[-100*ones(Len,1) Allx 100*ones(Len,1)];
clf;

histlen=size(Allx,2);
for cnt=1:Len
    h=plot3(dist(cnt)*ones(histlen,1), Allx(cnt,:), Alln(cnt,:));
    set(h,'LineWidth',3);
    hold on;
end
xlabel('Dist.');
ylabel('Light val.');
zlabel('Num. Samp.');
title('Histogram of light sensor noise.');
grid on;

ltvar=linspace(-100,100,50);
distvar=linspace(0,55);
HistSurf=griddata(dist'*ones(1,histlen), Allx, Alln, distvar', ltvar, 'cubic');
surf(distvar, ltvar, HistSurf);
% contour(distvar, ltvar, HistSurf);
% xlabel('Dist.');ylabel('Light value'); zlabel('Num. Samp.');
xlabel('Dist.');
ylabel('Light val.');
zlabel('Num. Samp.');
title('Histogram of light sensor noise.');
figFont(8);


figure(9);
Halogen35w=[...
7653        6707        6591        5718        4803        4168        3940        3661        2880	2716        2581        2587        2526        2539;
7574        6689        6561        5676        4907        4211        3912        3680        2886	2734        2606        2563        2502        2490;
7537        6738        6585        5731        4815        4187        3942        3662        2911	2728        2618        2600        2532        2545;
7513        6781        6561        5633        4910        4132        3881        3675        2911	2722        2636        2597        2520        2502;
7421        6787        6597        5732        4803        4187        3918        3668        2905	2734        2618        2600        2545        2545;
7415        6799        6561        5739        4865        4205        3912        3668        2911	2703        2600        2557        2532        2508;
7385        6805        6555        5682        4846        4162        3900        3656        2893	2722        2630        2600        2508        2514;
7366        6854        6561        5633        4882        4150        3918        3643        2893	2728        2624        2581        2539        2496;
7348        6829        6610        5761        4876        4162        3930        3668        2899	2716        2630        2606        2557        2532;
7342        6878        6573        5639        4827        4205        3969        3631        2929	2728        2624        2557        2508        2471];

R35=corrcoef(Halogen35w);