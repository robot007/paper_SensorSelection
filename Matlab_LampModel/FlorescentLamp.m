close all; clear all;

FlorescentDat=[7025        5072        2801        2334        2722        2709        2600        2551;
        6780        5773        3808        2600        2758        2807        2514        2551;
        6066        3314        3399        3552        3155        2606        2563        2435;
        7342        5810        4315        3039        2795        2746        2404        2581;
        6756        6390        3948        3039        3118        2655        2630        2484;
        6886        5230        3076        3332        2960        2429        2423        2600;
        7061        6237        4650        3759        2905        2484        2551        2526;
        7159        6393        3033        3167        3106        2764        2624        2508;
        6213        5584        4132        3594        2447        2362        2429        2600;
        4833        6146        2783        3033        3106        2825        2679        2532;
        4017        5700        3887        3033        2496        2404        2502        2557;
        5285        3814        4309        3552        2539        2722        2539        2557;
        7232        6390        3588        3597        2996        2722        2673        2593;
        6994        3717        4193        3594        2740        2490        2502        2490;
        6732        6188        2923        3527        2850        2728        2496        2386;
        7135        3533        2917        3527        3088        2539        2679        2557;
        6665        5950        3948        3723        3057        2777        2526        2453;
        7360        6091        3570        3222        2673        2655        2624        2545;
        6732        6091        3112        3344        3076        2386        2612        2441;
        7171        4479        4486        3216        2862        2783        2416        2551];
dist=[12.7 25.4 38.1 50.8 63.5 76.2 88.9 101.6];

figure(1);
CoSuf=corrcoef(FlorescentDat);
ltvar=linspace(-100,100,50);

surf(dist, dist, CoSuf);
xlabel('Distance (cm)'); 
ylabel('Distance (cm)'); 
zlabel('Corr. Coef.');
title('The correlation coefficients of Tmote Sky light sensors under a florescent lamp.', 'FontSize',14);
figFont(1);

mean(mean(abs(CoSuf)))

dat=FlorescentDat;
figure(2);
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

subplot(1,2,1);
histlen=size(Allx,2);
for cnt=1:Len
    h=plot3(dist(cnt)*ones(histlen,1), Allx(cnt,:), Alln(cnt,:));
    set(h,'LineWidth',3);
    hold on;
end
xlabel('Dist. (cm)');
ylabel('Light val.');
zlabel('Num. Samp.');
axis([0 120 -2000 2000 0 10]);
title('Histogram of light sensor noise. (under a florescent lamp)','FontSize',14);
figFont(2);
grid on;

subplot(1,2,2);
ltvar=linspace(-1500,1500,30);
distvar=linspace(0,120,30);
HistSurf=griddata(dist'*ones(1,histlen), Allx, Alln, distvar', ltvar, 'cubic');
surf(distvar, ltvar, HistSurf);
axis([0 120 -2000 2000 0 10]);
% contour(distvar, ltvar, HistSurf);
% xlabel('Dist.');ylabel('Light value'); zlabel('Num. Samp.');
xlabel('Dist. (cm)');
ylabel('Light val.');
zlabel('Num. Samp.');
figFont(2);

figure(3)
